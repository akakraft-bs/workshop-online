import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
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
    MatSlideToggleModule,
    MatDividerModule,
    MatIconModule,
    MatTooltipModule,
  ],
  templateUrl: 'profile-dialog.component.html',
  styleUrl: 'profile-dialog.component.scss'
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
    notifyUmfragen: [true],
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
          notifyUmfragen: prefs.notifyUmfragen,
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
      notifyUmfragen: this.form.get('notifyUmfragen')!.value ?? true,
    }).subscribe({
      next: () => { this.saving.set(false); this.auth.refreshCurrentUser(); this.dialogRef.close(true); },
      error: () => this.saving.set(false),
    });
  }
}
