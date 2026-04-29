import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/api/api.service';
import { AmtsTraeger } from '../../../models/verein.models';

@Component({
  selector: 'app-amtstraeger-edit-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatIconModule, MatProgressSpinnerModule,
  ],
  template: `
    <h2 mat-dialog-title>Kontaktdaten: {{ data.roleLabel }}</h2>
    <mat-dialog-content>
      <p class="name-hint">{{ data.userName }}</p>
      <form [formGroup]="form" class="form">
        <mat-form-field appearance="outline">
          <mat-label>Handynummer</mat-label>
          <mat-icon matPrefix>phone</mat-icon>
          <input matInput formControlName="phone" placeholder="+49 151 12345678">
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Adresse</mat-label>
          <mat-icon matPrefix>home</mat-icon>
          <textarea matInput formControlName="address" rows="2"
            placeholder="Musterstraße 1, 12345 Musterstadt"></textarea>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close()" [disabled]="saving()">Abbrechen</button>
      <button mat-raised-button color="primary" (click)="save()" [disabled]="saving()">
        @if (saving()) { <mat-spinner diameter="18" /> } @else { Speichern }
      </button>
    </mat-dialog-actions>
  `,
  styles: [`.form { display: flex; flex-direction: column; gap: 4px; padding-top: 8px; }
            .name-hint { font-weight: 600; margin: 0 0 12px; }`],
})
export class AmtsTraegerEditDialogComponent {
  private readonly api      = inject(ApiService);
  private readonly fb       = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  readonly dialogRef        = inject(MatDialogRef<AmtsTraegerEditDialogComponent>);
  readonly data: AmtsTraeger = inject(MAT_DIALOG_DATA);

  readonly saving = signal(false);
  readonly form = this.fb.nonNullable.group({
    phone:   [this.data.phone   ?? ''],
    address: [this.data.address ?? ''],
  });

  save(): void {
    this.saving.set(true);
    const { phone, address } = this.form.getRawValue();
    this.api.put<AmtsTraeger>(`/verein/info/amtstraeger/${this.data.userId}`, {
      phone:   phone   || null,
      address: address || null,
    }).subscribe({
      next: updated => this.dialogRef.close(updated),
      error: () => {
        this.snackBar.open('Speichern fehlgeschlagen.', 'OK', { duration: 3000 });
        this.saving.set(false);
      },
    });
  }
}
