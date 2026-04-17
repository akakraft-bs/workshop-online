import { Component, inject, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { PushNotificationService } from '../../core/push/push-notification.service';

@Component({
  selector: 'app-ios-install-banner',
  imports: [MatButtonModule, MatIconModule],
  template: `
    <div class="banner">
      <mat-icon class="banner-icon">ios_share</mat-icon>
      <div class="banner-text">
        <strong>Benachrichtigungen aktivieren</strong>
        <span>
          Tippe auf
          <mat-icon class="inline-icon">ios_share</mat-icon>
          und wähle <em>„Zum Home-Bildschirm"</em> um Push-Benachrichtigungen zu erhalten.
        </span>
      </div>
      <button mat-icon-button (click)="close()" aria-label="Schließen">
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
      align-items: flex-start;
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
      margin-top: 2px;
    }
    .banner-text {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 4px;
      font-size: 14px;
      line-height: 1.4;
    }
    .inline-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
      vertical-align: middle;
    }
  `],
})
export class IosInstallBannerComponent {
  private readonly push = inject(PushNotificationService);
  readonly closed = output<void>();

  close(): void {
    this.push.markIosHintSeen();
    this.closed.emit();
  }
}
