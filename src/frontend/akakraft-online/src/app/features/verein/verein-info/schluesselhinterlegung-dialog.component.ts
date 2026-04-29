import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/api/api.service';
import { Schluesselhinterlegung } from '../../../models/verein.models';

@Component({
  selector: 'app-schluesselhinterlegung-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatIconModule, MatProgressSpinnerModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data ? 'Eintrag bearbeiten' : 'Schlüsselhinterlegung hinzufügen' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form">
        <mat-form-field appearance="outline">
          <mat-label>Name</mat-label>
          <input matInput formControlName="name" placeholder="Max Mustermann">
          @if (form.controls.name.hasError('required') && form.controls.name.touched) {
            <mat-error>Name ist erforderlich</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Adresse</mat-label>
          <mat-icon matPrefix>home</mat-icon>
          <textarea matInput formControlName="address" rows="2"
            placeholder="Musterstraße 1, 12345 Musterstadt"></textarea>
          @if (form.controls.address.hasError('required') && form.controls.address.touched) {
            <mat-error>Adresse ist erforderlich</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Handynummer (optional)</mat-label>
          <mat-icon matPrefix>phone</mat-icon>
          <input matInput formControlName="phone" placeholder="+49 151 12345678">
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close()" [disabled]="saving()">Abbrechen</button>
      <button mat-raised-button color="primary" (click)="save()" [disabled]="saving()">
        @if (saving()) { <mat-spinner diameter="18" /> } @else { {{ data ? 'Speichern' : 'Anlegen' }} }
      </button>
    </mat-dialog-actions>
  `,
  styles: [`.form { display: flex; flex-direction: column; gap: 4px; padding-top: 8px; }`],
})
export class SchluesselhinterlegungDialogComponent {
  private readonly api      = inject(ApiService);
  private readonly fb       = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  readonly dialogRef        = inject(MatDialogRef<SchluesselhinterlegungDialogComponent>);
  readonly data: Schluesselhinterlegung | null = inject(MAT_DIALOG_DATA);

  readonly saving = signal(false);
  readonly form = this.fb.nonNullable.group({
    name:    [this.data?.name    ?? '', Validators.required],
    address: [this.data?.address ?? '', Validators.required],
    phone:   [this.data?.phone   ?? ''],
  });

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    const { name, address, phone } = this.form.getRawValue();
    const body = { name, address, phone: phone || null };
    const req$ = this.data
      ? this.api.put<Schluesselhinterlegung>(`/verein/info/schluessel/${this.data.id}`, body)
      : this.api.post<Schluesselhinterlegung>('/verein/info/schluessel', body);
    req$.subscribe({
      next: result => this.dialogRef.close(result),
      error: () => {
        this.snackBar.open('Speichern fehlgeschlagen.', 'OK', { duration: 3000 });
        this.saving.set(false);
      },
    });
  }
}
