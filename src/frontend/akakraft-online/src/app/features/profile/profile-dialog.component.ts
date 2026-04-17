import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../core/auth/auth.service';
import { Role } from '../../models/user.model';
import { UserPreferences, UserPreferencesService } from '../../core/user/user-preferences.service';
import { PushNotificationService } from '../../core/push/push-notification.service';

@Component({
  selector: 'app-profile-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatCheckboxModule,
    MatDividerModule,
    MatIconModule,
    MatTooltipModule,
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

        <mat-divider class="section-divider" />

        <div class="section-header">
          <mat-icon class="section-icon">notifications</mat-icon>
          <span class="section-title">Benachrichtigungen</span>
        </div>

        @if (push.isSupported()) {
          <div class="push-row">
            @if (push.permissionState() === 'denied') {
              <div class="push-denied">
                <mat-icon class="push-denied-icon">notifications_off</mat-icon>
                <span>
                  Push-Benachrichtigungen wurden im Browser blockiert.
                  Bitte erlaube sie in den Browser-Einstellungen und lade die Seite neu.
                </span>
              </div>
            } @else {
              <button
                mat-stroked-button
                [color]="push.isSubscribed() ? 'warn' : 'primary'"
                (click)="togglePush()"
                [disabled]="togglingPush()">
                @if (togglingPush()) {
                  <mat-spinner diameter="18" />
                } @else if (push.isSubscribed()) {
                  <mat-icon>notifications_off</mat-icon>
                  Push deaktivieren
                } @else {
                  <mat-icon>notifications_active</mat-icon>
                  Push aktivieren
                }
              </button>
              @if (push.isSubscribed()) {
                <span class="push-active-hint">
                  <mat-icon class="push-check-icon">check_circle</mat-icon>
                  Aktiv auf diesem Gerät
                </span>
              }
            }
          </div>
        } @else {
          <p class="push-unsupported">
            Dein Browser unterstützt keine Push-Benachrichtigungen.
          </p>
        }

        <div class="notification-list">
          <div class="notification-item">
            <mat-checkbox formControlName="notifyLeihruckgabe">
              Leihrückgabe
            </mat-checkbox>
            <p class="notification-hint">
              Erinnerung wenn ein ausgeliehenes Werkzeug nach dem Rückgabedatum noch nicht zurückgegeben wurde.
            </p>
          </div>

          <div class="notification-item">
            <mat-checkbox formControlName="notifyVeranstaltungen">
              Veranstaltungen
            </mat-checkbox>
            <p class="notification-hint">
              Benachrichtigung bei neu eingestellten Veranstaltungen und eine Erinnerung je einen Tag vorher.
            </p>
          </div>

          @if (isHallenwart()) {
            <div class="notification-item">
              <mat-checkbox formControlName="notifyMindestbestand">
                Mindestbestand-Unterschreitung
              </mat-checkbox>
              <p class="notification-hint">
                Benachrichtigung wenn ein Verbrauchsmittel den konfigurierten Mindestbestand unterschreitet.
              </p>
            </div>
          }
        </div>

      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Abbrechen</button>
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
    .profile-form {
      display: flex;
      flex-direction: column;
      min-width: 320px;
    }
    mat-form-field { width: 100%; }
    mat-spinner { display: inline-flex; }
    .section-divider { margin: 16px 0 20px; }
    .section-header {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 16px;
    }
    .section-icon {
      font-size: 20px;
      width: 20px;
      height: 20px;
      color: var(--mat-sys-on-surface-variant);
    }
    .section-title {
      font-size: 14px;
      font-weight: 500;
      color: var(--mat-sys-on-surface-variant);
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }
    .push-row {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 16px;
      flex-wrap: wrap;
    }
    .push-active-hint {
      display: flex;
      align-items: center;
      gap: 4px;
      font-size: 13px;
      color: var(--mat-sys-primary);
    }
    .push-check-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
    }
    .push-denied {
      display: flex;
      align-items: flex-start;
      gap: 8px;
      font-size: 13px;
      color: var(--mat-sys-error);
      line-height: 1.4;
    }
    .push-denied-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
      flex-shrink: 0;
      margin-top: 1px;
    }
    .push-unsupported {
      font-size: 13px;
      color: var(--mat-sys-on-surface-variant);
      margin: 0 0 16px;
    }
    .notification-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }
    .notification-item {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }
    .notification-hint {
      font-size: 12px;
      color: var(--mat-sys-on-surface-variant);
      margin: 0 0 0 32px;
      line-height: 1.4;
    }
  `],
})
export class ProfileDialogComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly push = inject(PushNotificationService);
  private readonly prefsService = inject(UserPreferencesService);
  private readonly dialogRef = inject(MatDialogRef<ProfileDialogComponent>);
  private readonly fb = inject(FormBuilder);

  readonly saving = signal(false);
  readonly togglingPush = signal(false);
  private loadedPrefs: UserPreferences | null = null;

  readonly isHallenwart = () =>
    this.auth.hasAnyRole([Role.Hallenwart, Role.Admin, Role.Chairman, Role.ViceChairman]);

  readonly form = this.fb.group({
    displayName: ['', [Validators.maxLength(64)]],
    notifyLeihruckgabe: [true],
    notifyVeranstaltungen: [true],
    notifyMindestbestand: [true],
  });

  previewName(): string {
    const v = this.form.get('displayName')!.value?.trim();
    return v || this.auth.currentUser()?.name?.split(' ')[0] || 'Name';
  }

  ngOnInit(): void {
    this.prefsService.getPreferences().subscribe({
      next: prefs => {
        this.loadedPrefs = prefs;
        this.form.patchValue({
          displayName: prefs.displayName ?? '',
          notifyLeihruckgabe: prefs.notifyLeihruckgabe,
          notifyVeranstaltungen: prefs.notifyVeranstaltungen,
          notifyMindestbestand: prefs.notifyMindestbestand,
        });
        this.form.markAsPristine();
      },
    });
  }

  togglePush(): void {
    if (this.togglingPush()) return;
    this.togglingPush.set(true);

    const action$ = this.push.isSubscribed()
      ? this.push.disable()
      : this.push.enable();

    action$.subscribe({
      next: () => this.togglingPush.set(false),
      error: () => this.togglingPush.set(false),
    });
  }

  save(): void {
    if (this.form.invalid || this.saving()) return;
    this.saving.set(true);

    const displayName = this.form.get('displayName')!.value?.trim() || null;
    const favoriteRoutes = this.loadedPrefs?.favoriteRoutes ?? [];

    this.prefsService.updatePreferences({
      favoriteRoutes,
      displayName,
      notifyLeihruckgabe: this.form.get('notifyLeihruckgabe')!.value ?? true,
      notifyVeranstaltungen: this.form.get('notifyVeranstaltungen')!.value ?? true,
      notifyMindestbestand: this.form.get('notifyMindestbestand')!.value ?? true,
    }).subscribe({
      next: () => { this.saving.set(false); this.dialogRef.close(true); },
      error: () => this.saving.set(false),
    });
  }
}
