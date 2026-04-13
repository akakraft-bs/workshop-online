import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../../core/api/api.service';
import { Werkzeug } from '../../../models/werkzeug.model';

export interface AusleihenDialogData {
  werkzeug: Werkzeug;
}

@Component({
  selector: 'app-ausleihen-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule, MatButtonModule, MatFormFieldModule,
    MatInputModule, MatProgressSpinnerModule,
  ],
  templateUrl: './ausleihen-dialog.component.html',
  styleUrl: './ausleihen-dialog.component.scss',
})
export class AusleihenDialogComponent {
  private readonly api = inject(ApiService);
  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject(MatDialogRef<AusleihenDialogComponent>);
  readonly data: AusleihenDialogData = inject(MAT_DIALOG_DATA);

  readonly saving = signal(false);

  // Minimum: morgen
  readonly minDate = (() => {
    const d = new Date();
    d.setDate(d.getDate() + 1);
    return d.toISOString().slice(0, 10);
  })();

  readonly form = this.fb.nonNullable.group({
    expectedReturnAt: [this.minDate, Validators.required],
  });

  confirm(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const dateValue = this.form.getRawValue().expectedReturnAt;
    // Datum in UTC-Mitternacht umwandeln
    const expectedReturnAt = new Date(dateValue + 'T00:00:00Z').toISOString();

    this.api
      .post<Werkzeug>(`/werkzeug/${this.data.werkzeug.id}/ausleihen`, { expectedReturnAt })
      .subscribe({
        next: result => this.dialogRef.close(result),
        error: () => {
          this.snackBar.open('Ausleihe fehlgeschlagen.', 'OK', { duration: 3000 });
          this.saving.set(false);
        },
      });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
