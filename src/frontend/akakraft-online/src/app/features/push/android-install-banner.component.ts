import { Component, inject, output, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { PushNotificationService } from '../../core/push/push-notification.service';

@Component({
  selector: 'app-android-install-banner',
  imports: [MatButtonModule, MatIconModule],
  template: `
    <div class="banner">
      <mat-icon class="banner-icon">install_mobile</mat-icon>
      <div class="banner-text">
        <strong>App installieren</strong>
        <span>Installiere AkaKraft als App für schnelleren Zugriff und Push-Benachrichtigungen.</span>
      </div>
      <div class="banner-actions">
        <button mat-button (click)="dismiss()" [disabled]="installing()">Später</button>
        <button mat-flat-button color="primary" (click)="install()" [disabled]="installing()">
          Installieren
        </button>
      </div>
      <button mat-icon-button class="close-btn" (click)="dismiss()" aria-label="Schließen">
        <mat-icon>close</mat-icon>
      </button>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      position: fixed;
      bottom: 0;
      left: 0;
      right: 0;
      z-index: 1000;
      padding: 0 16px 16px;
      animation: slide-up 0.3s ease-out;
    }
    @keyframes slide-up {
      from { transform: translateY(100%); opacity: 0; }
      to   { transform: translateY(0);    opacity: 1; }
    }
    .banner {
      display: flex;
      align-items: center;
      gap: 12px;
      background: var(--mat-sys-surface-container-high, #fff);
      border-radius: 16px;
      padding: 16px;
      box-shadow: 0 4px 20px rgba(0,0,0,0.15);
    }
    .banner-icon {
      font-size: 28px;
      width: 28px;
      height: 28px;
      color: var(--mat-sys-primary);
      flex-shrink: 0;
    }
    .banner-text {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 2px;
      font-size: 14px;
      line-height: 1.4;
    }
    .banner-actions {
      display: flex;
      gap: 4px;
      flex-shrink: 0;
    }
    .close-btn {
      flex-shrink: 0;
    }
  `],
})
export class AndroidInstallBannerComponent {
  private readonly push = inject(PushNotificationService);
  readonly closed = output<void>();

  readonly installing = signal(false);

  async install(): Promise<void> {
    this.installing.set(true);
    await this.push.triggerInstall();
    this.installing.set(false);
    this.closed.emit();
  }

  dismiss(): void {
    this.push.markAndroidInstallSeen();
    this.closed.emit();
  }
}
