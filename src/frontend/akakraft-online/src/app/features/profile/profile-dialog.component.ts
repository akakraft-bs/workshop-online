import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../core/auth/auth.service';
import { UserPreferencesService } from '../../core/user/user-preferences.service';
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
  private loadedPrefs: { favoriteRoutes: string[] } | null = null;

  readonly form = this.fb.group({
    displayName: ['', [Validators.maxLength(64)]],
    phone: ['', [Validators.maxLength(64)]],
    address: ['', [Validators.maxLength(512)]],
  });

  ngOnInit(): void {
    this.prefsService.getPreferences().subscribe({
      next: prefs => {
        this.loadedPrefs = prefs;
        this.form.patchValue({
          displayName: prefs.displayName ?? '',
          phone: prefs.phone ?? '',
          address: prefs.address ?? '',
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
    const phone = this.form.get('phone')!.value?.trim() || null;
    const address = this.form.get('address')!.value?.trim() || null;
    const favoriteRoutes = this.loadedPrefs?.favoriteRoutes ?? [];

    this.prefsService.updatePreferences({
      favoriteRoutes,
      displayName,
      phone,
      address,
    }).subscribe({
      next: () => { this.saving.set(false); this.auth.refreshCurrentUser(); this.dialogRef.close(true); },
      error: () => this.saving.set(false),
    });
  }
}
