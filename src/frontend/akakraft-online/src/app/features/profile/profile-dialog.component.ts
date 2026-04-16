import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../core/auth/auth.service';
import { UserPreferencesService } from '../../core/user/user-preferences.service';

@Component({
  selector: 'app-profile-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
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
    .profile-form { display: flex; flex-direction: column; min-width: 320px; }
    mat-form-field { width: 100%; }
    mat-spinner { display: inline-flex; }
  `],
})
export class ProfileDialogComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly prefsService = inject(UserPreferencesService);
  private readonly dialogRef = inject(MatDialogRef<ProfileDialogComponent>);
  private readonly fb = inject(FormBuilder);

  readonly saving = signal(false);

  readonly form = this.fb.group({
    displayName: ['', [Validators.maxLength(64)]],
  });

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
