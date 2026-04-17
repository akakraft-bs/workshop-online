import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../api/api.service';

@Injectable({ providedIn: 'root' })
export class PushNotificationService {
  private readonly api = inject(ApiService);

  get isSupported(): boolean {
    return 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;
  }

  get permissionState(): NotificationPermission {
    return Notification.permission;
  }

  async registerServiceWorker(): Promise<ServiceWorkerRegistration | null> {
    if (!this.isSupported) return null;
    try {
      return await navigator.serviceWorker.register('/push-sw.js', { scope: '/' });
    } catch {
      return null;
    }
  }

  async getCurrentSubscription(): Promise<PushSubscription | null> {
    if (!this.isSupported) return null;
    const reg = await navigator.serviceWorker.getRegistration('/push-sw.js');
    if (!reg) return null;
    return reg.pushManager.getSubscription();
  }

  async isSubscribed(): Promise<boolean> {
    const sub = await this.getCurrentSubscription();
    return sub !== null;
  }

  async subscribe(): Promise<boolean> {
    if (!this.isSupported) return false;

    const permission = await Notification.requestPermission();
    if (permission !== 'granted') return false;

    try {
      const reg = await this.registerServiceWorker();
      if (!reg) return false;

      const { publicKey } = await firstValueFrom(
        this.api.get<{ publicKey: string }>('/push/vapid-public-key')
      );

      const existing = await reg.pushManager.getSubscription();
      if (existing) {
        await this.sendSubscriptionToServer(existing);
        return true;
      }

      const subscription = await reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(publicKey),
      });

      await this.sendSubscriptionToServer(subscription);
      return true;
    } catch {
      return false;
    }
  }

  async unsubscribe(): Promise<boolean> {
    try {
      const subscription = await this.getCurrentSubscription();
      if (!subscription) return true;

      await firstValueFrom(
        this.api.post('/push/unsubscribe', {
          endpoint: subscription.endpoint,
          p256DH: subscription.toJSON().keys?.['p256dh'] ?? '',
          auth: subscription.toJSON().keys?.['auth'] ?? '',
        })
      ).catch(() => {});

      return subscription.unsubscribe();
    } catch {
      return false;
    }
  }

  private async sendSubscriptionToServer(sub: PushSubscription): Promise<void> {
    const keys = sub.toJSON().keys ?? {};
    await firstValueFrom(
      this.api.post('/push/subscribe', {
        endpoint: sub.endpoint,
        p256DH: keys['p256dh'] ?? '',
        auth: keys['auth'] ?? '',
      })
    );
  }
}

function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
  const rawData = atob(base64);
  return Uint8Array.from(rawData, c => c.charCodeAt(0));
}
