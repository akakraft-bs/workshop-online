import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/api/api.service';
import { VereinZugang } from '../../../models/verein.models';

@Component({
  selector: 'app-zugang-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatProgressSpinnerModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data ? 'Zugang bearbeiten' : 'Zugang hinzufügen' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form">
        <mat-form-field appearance="outline">
          <mat-label>Anbieter</mat-label>
          <input matInput formControlName="anbieter" placeholder="z. B. Teilehändler, Online Dienste ...">
          @if (form.controls.anbieter.hasError('required') && form.controls.anbieter.touched) {
            <mat-error>Anbieter ist erforderlich</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Zugangsdaten</mat-label>
          <textarea matInput formControlName="zugangsdaten" rows="6"
            placeholder="Benutzername, Passwort, PIN ..."></textarea>
          @if (form.controls.zugangsdaten.hasError('required') && form.controls.zugangsdaten.touched) {
            <mat-error>Zugangsdaten sind erforderlich</mat-error>
          }
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
  styles: [`.form { display: flex; flex-direction: column; gap: 4px; padding-top: 8px; min-width: 400px; }`],
})
export class ZugangDialogComponent {
  private readonly api      = inject(ApiService);
  private readonly fb       = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  readonly dialogRef        = inject(MatDialogRef<ZugangDialogComponent>);
  readonly data: VereinZugang | null = inject(MAT_DIALOG_DATA);

  readonly saving = signal(false);
  readonly form = this.fb.nonNullable.group({
    anbieter:     [this.data?.anbieter     ?? '', Validators.required],
    zugangsdaten: [this.data?.zugangsdaten ?? '', Validators.required],
  });

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    const body = this.form.getRawValue();
    const req$ = this.data
      ? this.api.put<VereinZugang>(`/verein/zugaenge/${this.data.id}`, body)
      : this.api.post<VereinZugang>('/verein/zugaenge', body);
    req$.subscribe({
      next: result => this.dialogRef.close(result),
      error: () => {
        this.snackBar.open('Speichern fehlgeschlagen.', 'OK', { duration: 3000 });
        this.saving.set(false);
      },
    });
  }
}
