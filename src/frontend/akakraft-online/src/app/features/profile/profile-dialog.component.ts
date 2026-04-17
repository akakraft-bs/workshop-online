import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { AuthService } from '../../core/auth/auth.service';
import { NotificationPreferencesService } from '../../core/push/notification-preferences.service';
import { PushNotificationService } from '../../core/push/push-notification.service';
import { UserPreferencesService } from '../../core/user/user-preferences.service';
import { Role } from '../../models/user.model';

@Component({
  selector: 'app-profile-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule,
    MatDividerModule,
    MatIconModule,
  ],
  template: `
    <h2 mat-dialog-title>Profil</h2>
    <mat-dialog-content>
      <div class="user-info">
        @if (auth.currentUser()?.pictureUrl; as pic) {
          <img class="avatar" [src]="pic" [alt]="auth.currentUser()?.name">
        }
        <div>
          <div class="real-name">{{ auth.currentUser()?.name }}</div>
          <div class="email">{{ auth.currentUser()?.email }}</div>
        </div>
      </div>

      <form [formGroup]="form" class="profile-form">
        <mat-form-field appearance="outline">
          <mat-label>Anzeigename</mat-label>
          <input matInput formControlName="displayName"
                 placeholder="z. B. dein Vorname oder Spitzname">
          <mat-hint>
            Wird als Präfix bei Hallenbelegungseinträgen verwendet:
            <em>{{ previewName() }} – Titel</em>
          </mat-hint>
        </mat-form-field>
      </form>

      <mat-divider class="section-divider" />

      <div class="section-title">
        <mat-icon>notifications</mat-icon>
        Push-Benachrichtigungen
      </div>

      @if (!pushService.isSupported) {
        <p class="hint-text">Dein Browser unterstützt keine Push-Benachrichtigungen.</p>
      } @else {
        <div class="push-row">
          <div class="push-info">
            @if (pushPermission() === 'granted' && isSubscribed()) {
              <span class="push-status active">
                <mat-icon>check_circle</mat-icon> Aktiv
              </span>
            } @else if (pushPermission() === 'denied') {
              <span class="push-status denied">
                <mat-icon>block</mat-icon> Vom Browser blockiert
              </span>
            } @else {
              <span class="push-status inactive">
                <mat-icon>notifications_off</mat-icon> Nicht aktiviert
              </span>
            }
          </div>
          <div class="push-buttons">
            @if (isSubscribed()) {
              <button mat-stroked-button color="warn"
                      [disabled]="pushLoading()"
                      (click)="unsubscribePush()">
                Deaktivieren
              </button>
            } @else {
              <button mat-flat-button color="primary"
                      [disabled]="pushLoading() || pushPermission() === 'denied'"
                      (click)="subscribePush()">
                @if (pushLoading()) { <mat-spinner diameter="18" /> }
                @else { Aktivieren }
              </button>
            }
          </div>
        </div>

        @if (isSubscribed()) {
          <div class="notif-prefs">
            <mat-slide-toggle
              [checked]="notifPrefs().werkzeugRueckgabe"
              (change)="updatePref('werkzeugRueckgabe', $event.checked)"
              [disabled]="savingPrefs()">
              Werkzeug-Rückgabe-Erinnerungen
            </mat-slide-toggle>

            <mat-slide-toggle
              [checked]="notifPrefs().veranstaltungen"
              (change)="updatePref('veranstaltungen', $event.checked)"
              [disabled]="savingPrefs()">
              Neue Veranstaltungen
            </mat-slide-toggle>

            @if (isHallenwart()) {
              <mat-slide-toggle
                [checked]="notifPrefs().verbrauchsmaterialMindestbestand"
                (change)="updatePref('verbrauchsmaterialMindestbestand', $event.checked)"
                [disabled]="savingPrefs()">
                Verbrauchsmaterial-Mindestbestand
              </mat-slide-toggle>
            }
          </div>
        }
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Schließen</button>
      <button mat-flat-button color="primary"
              (click)="save()"
              [disabled]="saving() || form.pristine">
        @if (saving()) {
          <mat-spinner diameter="18" />
        } @else {
          Speichern
        }
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .user-info {
      display: flex;
      align-items: center;
      gap: 14px;
      margin-bottom: 20px;
    }
    .avatar {
      width: 48px;
      height: 48px;
      border-radius: 50%;
      object-fit: cover;
    }
    .real-name { font-weight: 500; font-size: 15px; }
    .email { font-size: 13px; color: var(--mat-sys-on-surface-variant); }
    .profile-form { display: flex; flex-direction: column; min-width: 320px; }
    mat-form-field { width: 100%; }
    mat-spinner { display: inline-flex; }

    .section-divider { margin: 20px 0 16px; }

    .section-title {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 14px;
      font-weight: 500;
      margin-bottom: 12px;
      color: var(--mat-sys-on-surface-variant);
    }

    .push-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 12px;
    }

    .push-status {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 13px;
    }
    .push-status mat-icon { font-size: 16px; width: 16px; height: 16px; }
    .push-status.active { color: var(--mat-sys-tertiary); }
    .push-status.denied { color: var(--mat-sys-error); }
    .push-status.inactive { color: var(--mat-sys-on-surface-variant); }

    .notif-prefs {
      display: flex;
      flex-direction: column;
      gap: 10px;
      padding: 4px 0 8px;
    }

    .hint-text {
      font-size: 13px;
      color: var(--mat-sys-on-surface-variant);
    }
  `],
})
export class ProfileDialogComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly pushService = inject(PushNotificationService);
  private readonly prefsService = inject(UserPreferencesService);
  private readonly notifPrefsService = inject(NotificationPreferencesService);
  private readonly dialogRef = inject(MatDialogRef<ProfileDialogComponent>);
  private readonly fb = inject(FormBuilder);

  readonly saving = signal(false);
  readonly pushLoading = signal(false);
  readonly savingPrefs = signal(false);
  readonly isSubscribed = signal(false);
  readonly pushPermission = signal<NotificationPermission>('default');
  readonly notifPrefs = signal({
    werkzeugRueckgabe: true,
    veranstaltungen: true,
    verbrauchsmaterialMindestbestand: false,
  });

  readonly form = this.fb.group({
    displayName: ['', [Validators.maxLength(64)]],
  });

  isHallenwart(): boolean {
    return this.auth.currentUser()?.roles.includes(Role.Hallenwart) ?? false;
  }

  previewName(): string {
    const v = this.form.get('displayName')!.value?.trim();
    return v || this.auth.currentUser()?.name?.split(' ')[0] || 'Name';
  }

  ngOnInit(): void {
    this.prefsService.getPreferences().subscribe({
      next: prefs => {
        this.form.patchValue({ displayName: prefs.displayName ?? '' });
        this.form.markAsPristine();
      },
    });

    this.notifPrefsService.getPreferences().subscribe({
      next: prefs => this.notifPrefs.set(prefs),
    });

    if (this.pushService.isSupported) {
      this.pushPermission.set(this.pushService.permissionState);
      this.pushService.isSubscribed().then(v => this.isSubscribed.set(v));
    }
  }

  async subscribePush(): Promise<void> {
    this.pushLoading.set(true);
    const success = await this.pushService.subscribe();
    if (success) {
      this.isSubscribed.set(true);
      this.pushPermission.set('granted');
    }
    this.pushLoading.set(false);
  }

  async unsubscribePush(): Promise<void> {
    this.pushLoading.set(true);
    const success = await this.pushService.unsubscribe();
    if (success) this.isSubscribed.set(false);
    this.pushLoading.set(false);
  }

  updatePref(
    key: 'werkzeugRueckgabe' | 'veranstaltungen' | 'verbrauchsmaterialMindestbestand',
    value: boolean
  ): void {
    const updated = { ...this.notifPrefs(), [key]: value };
    this.notifPrefs.set(updated);
    this.savingPrefs.set(true);
    this.notifPrefsService.updatePreferences(updated).subscribe({
      next: saved => { this.notifPrefs.set(saved); this.savingPrefs.set(false); },
      error: () => this.savingPrefs.set(false),
    });
  }

  save(): void {
    if (this.form.invalid || this.saving()) return;
    this.saving.set(true);

    const displayName = this.form.get('displayName')!.value?.trim() || null;

    this.prefsService.getPreferences().subscribe({
      next: prefs => {
        this.prefsService.updatePreferences(prefs.favoriteRoutes, displayName).subscribe({
          next: () => { this.saving.set(false); this.dialogRef.close(true); },
          error: () => this.saving.set(false),
        });
      },
      error: () => {
        this.prefsService.updatePreferences([], displayName).subscribe({
          next: () => { this.saving.set(false); this.dialogRef.close(true); },
          error: () => this.saving.set(false),
        });
      },
    });
  }
}
