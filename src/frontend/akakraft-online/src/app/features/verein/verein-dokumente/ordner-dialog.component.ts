import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/api/api.service';
import { DokumentOrdnerDto } from '../../../models/verein.models';

@Component({
  selector: 'app-ordner-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatProgressSpinnerModule,
  ],
  template: `
    <h2 mat-dialog-title>Neuen Ordner anlegen</h2>
    <mat-dialog-content>
      <form [formGroup]="form" style="padding-top:8px">
        <mat-form-field appearance="outline" style="width:100%">
          <mat-label>Name</mat-label>
          <input matInput formControlName="name" placeholder="z. B. Satzungen">
          @if (form.controls.name.hasError('required') && form.controls.name.touched) {
            <mat-error>Name ist erforderlich</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close()" [disabled]="saving()">Abbrechen</button>
      <button mat-raised-button color="primary" (click)="save()" [disabled]="saving()">
        @if (saving()) { <mat-spinner diameter="18" /> } @else { Anlegen }
      </button>
    </mat-dialog-actions>
  `,
})
export class OrdnerDialogComponent {
  private readonly api      = inject(ApiService);
  private readonly fb       = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  readonly dialogRef        = inject(MatDialogRef<OrdnerDialogComponent>);

  readonly saving = signal(false);
  readonly form = this.fb.nonNullable.group({ name: ['', Validators.required] });

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    this.api.post<DokumentOrdnerDto>('/verein/dokumente/ordner', { name: this.form.value.name }).subscribe({
      next: result => this.dialogRef.close(result),
      error: () => { this.snackBar.open('Anlegen fehlgeschlagen.', 'OK', { duration: 3000 }); this.saving.set(false); },
    });
  }
}
