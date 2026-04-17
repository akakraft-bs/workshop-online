import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PushNotificationService } from '../../core/push/push-notification.service';

export type PushPromptResult = 'activated' | 'later' | 'never';

@Component({
  selector: 'app-push-prompt-dialog',
  imports: [MatDialogModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <div class="prompt-icon-row">
      <mat-icon class="bell-icon">notifications_active</mat-icon>
    </div>

    <h2 mat-dialog-title>Benachrichtigungen aktivieren?</h2>

    <mat-dialog-content>
      <p>Bleib immer auf dem Laufenden:</p>
      <ul>
        <li>Erinnerung wenn dein ausgeliehenes Werkzeug überfällig ist</li>
        <li>Neue Veranstaltungen und Erinnerungen einen Tag vorher</li>
      </ul>
      <p class="hint">
        Du kannst die Einstellungen jederzeit im Profil ändern.
      </p>
    </mat-dialog-content>

    <mat-dialog-actions>
      <button mat-button (click)="dismiss('never')">Nicht mehr fragen</button>
      <span class="spacer"></span>
      <button mat-button (click)="dismiss('later')">Später</button>
      <button
        mat-flat-button
        color="primary"
        (click)="activate()"
        [disabled]="activating()">
        @if (activating()) {
          <mat-spinner diameter="18" />
        } @else {
          <span class="btn-inner">
            <mat-icon>notifications_active</mat-icon>
            Aktivieren
          </span>
        }
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .prompt-icon-row {
      display: flex;
      justify-content: center;
      padding: 24px 24px 0;
    }
    .bell-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      color: var(--mat-sys-primary);
    }
    h2[mat-dialog-title] {
      text-align: center;
    }
    mat-dialog-content {
      max-width: 340px;
    }
    ul {
      margin: 8px 0 12px;
      padding-left: 20px;
      line-height: 1.7;
    }
    .hint {
      font-size: 12px;
      color: var(--mat-sys-on-surface-variant);
      margin: 0;
    }
    mat-dialog-actions {
      display: flex !important;
      flex-wrap: nowrap !important;
      align-items: center !important;
      gap: 4px;
    }
    .spacer { flex: 1; }
    mat-spinner { display: inline-flex; }
    .btn-inner {
      display: inline-flex;
      align-items: center;
      gap: 4px;
    }
    @media (max-width: 599px) {
      mat-dialog-actions {
        flex-direction: column-reverse !important;
        align-items: stretch !important;
        gap: 4px;
      }
      .spacer { display: none; }
    }
  `],
})
export class PushPromptDialogComponent {
  private readonly push = inject(PushNotificationService);
  private readonly dialogRef = inject(MatDialogRef<PushPromptDialogComponent, PushPromptResult>);

  readonly activating = signal(false);

  activate(): void {
    this.activating.set(true);
    this.push.markPromptSeen();
    this.push.enable().subscribe({
      next: success => {
        this.activating.set(false);
        this.dialogRef.close(success ? 'activated' : 'later');
      },
      error: () => {
        this.activating.set(false);
        this.dialogRef.close('later');
      },
    });
  }

  dismiss(result: PushPromptResult): void {
    if (result === 'never') this.push.markPromptSeen();
    this.dialogRef.close(result);
  }
}
