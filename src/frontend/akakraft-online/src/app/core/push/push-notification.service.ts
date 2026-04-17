import { inject, Injectable, signal } from '@angular/core';
import { initializeApp, getApps, FirebaseApp } from 'firebase/app';
import { getMessaging, getToken, deleteToken, Messaging, onMessage } from 'firebase/messaging';
import { from, Observable, of } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiService } from '../api/api.service';

export type PushPermissionState = 'default' | 'granted' | 'denied' | 'unsupported';

const TOKEN_STORAGE_KEY = 'fcm_token';
const PROMPT_SEEN_KEY = 'push_prompt_seen';
const IOS_HINT_SEEN_KEY = 'ios_install_hint_seen';

@Injectable({ providedIn: 'root' })
export class PushNotificationService {
  private readonly api = inject(ApiService);

  private app: FirebaseApp | null = null;
  private messaging: Messaging | null = null;

  /** Ob Push im Browser grundsätzlich erlaubt ist. */
  readonly permissionState = signal<PushPermissionState>(this.readCurrentPermission());

  /** Ob ein Token aktiv im Backend registriert ist (= Push "eingeschaltet"). */
  readonly isSubscribed = signal(!!localStorage.getItem(TOKEN_STORAGE_KEY));

  constructor() {
    if (this.isSupported()) {
      this.app = getApps().length
        ? getApps()[0]
        : initializeApp(environment.firebase);
      this.messaging = getMessaging(this.app);
    }
  }

  isSupported(): boolean {
    return typeof window !== 'undefined'
      && 'Notification' in window
      && 'serviceWorker' in navigator
      && 'PushManager' in window;
  }

  /** Fragt die Berechtigung an, holt den FCM-Token und speichert ihn im Backend. */
  enable(): Observable<boolean> {
    if (!this.isSupported() || !this.messaging) {
      return of(false);
    }

    return from(this.requestPermissionAndToken()).pipe(
      switchMap(token => {
        if (!token) return of(false);
        return this.api.post<unknown>('/push/tokens', { token }).pipe(
          tap(() => {
            localStorage.setItem(TOKEN_STORAGE_KEY, token);
            this.isSubscribed.set(true);
          }),
          switchMap(() => of(true)),
          catchError(() => of(false)),
        );
      }),
      catchError(() => of(false)),
    );
  }

  /** Löscht den FCM-Token aus dem Browser und im Backend. */
  disable(): Observable<boolean> {
    if (!this.isSupported() || !this.messaging) {
      return of(false);
    }

    const storedToken = localStorage.getItem(TOKEN_STORAGE_KEY);

    const removeFromBackend$ = storedToken
      ? this.api.delete<unknown>(`/push/tokens/${encodeURIComponent(storedToken)}`).pipe(
          catchError(() => of(null)),
        )
      : of(null);

    const deleteFromBrowser$ = from(
      deleteToken(this.messaging).catch(() => false)
    );

    return removeFromBackend$.pipe(
      switchMap(() => deleteFromBrowser$),
      tap(() => {
        localStorage.removeItem(TOKEN_STORAGE_KEY);
        this.isSubscribed.set(false);
      }),
      switchMap(() => of(true)),
      catchError(() => {
        localStorage.removeItem(TOKEN_STORAGE_KEY);
        this.isSubscribed.set(false);
        return of(false);
      }),
    );
  }

  /** Lauscht auf Vordergrund-Nachrichten und zeigt sie als Browser-Notification. */
  listenForeground(): void {
    if (!this.messaging) return;

    onMessage(this.messaging, payload => {
      const { title, body, icon } = payload.notification ?? {};
      if (Notification.permission === 'granted') {
        new Notification(title ?? 'AkaKraft', {
          body: body ?? '',
          icon: icon ?? '/app/android-chrome-192x192.png',
        });
      }
    });
  }

  private async requestPermissionAndToken(): Promise<string | null> {
    const permission = await Notification.requestPermission();
    this.permissionState.set(permission === 'granted' ? 'granted' : 'denied');
    if (permission !== 'granted') return null;
    return this.getFreshToken();
  }

  private async getFreshToken(): Promise<string | null> {
    if (!this.messaging) return null;
    try {
      const swReg = await navigator.serviceWorker.register(environment.swPath);
      return await getToken(this.messaging, {
        vapidKey: environment.vapidKey,
        serviceWorkerRegistration: swReg,
      });
    } catch {
      return null;
    }
  }

  /** Läuft die App als installierte PWA (Home-Bildschirm)? */
  isStandalone(): boolean {
    return window.matchMedia('(display-mode: standalone)').matches
      || (navigator as { standalone?: boolean }).standalone === true;
  }

  /** Läuft die App auf einem iOS-Gerät? */
  isIos(): boolean {
    return /iphone|ipad|ipod/i.test(navigator.userAgent);
  }

  /**
   * Soll der Push-Aktivierungs-Dialog gezeigt werden?
   * Ja wenn: Push unterstützt, noch nicht abonniert, noch nicht gefragt.
   * Auf iOS nur wenn die App als PWA läuft.
   */
  shouldShowPushPrompt(): boolean {
    if (this.isSubscribed()) return false;
    if (localStorage.getItem(PROMPT_SEEN_KEY)) return false;
    if (!this.isSupported()) return false;
    if (this.isIos() && !this.isStandalone()) return false;
    return true;
  }

  /**
   * Soll der iOS-Installationshinweis gezeigt werden?
   * Ja wenn: iOS-Gerät, nicht als PWA, Hinweis noch nicht gezeigt.
   */
  shouldShowIosInstallHint(): boolean {
    if (!this.isIos()) return false;
    if (this.isStandalone()) return false;
    if (localStorage.getItem(IOS_HINT_SEEN_KEY)) return false;
    return true;
  }

  markPromptSeen(): void {
    localStorage.setItem(PROMPT_SEEN_KEY, '1');
  }

  markIosHintSeen(): void {
    localStorage.setItem(IOS_HINT_SEEN_KEY, '1');
  }

  private readCurrentPermission(): PushPermissionState {
    if (typeof window === 'undefined' || !('Notification' in window)) return 'unsupported';
    return Notification.permission as PushPermissionState;
  }
}
