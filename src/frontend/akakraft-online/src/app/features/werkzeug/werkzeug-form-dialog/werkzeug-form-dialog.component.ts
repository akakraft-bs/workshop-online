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

export interface WerkzeugFormDialogData {
  werkzeug: Werkzeug | null;
}

@Component({
  selector: 'app-werkzeug-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule, MatButtonModule, MatFormFieldModule,
    MatInputModule, MatProgressSpinnerModule,
  ],
  templateUrl: './werkzeug-form-dialog.component.html',
  styleUrl: './werkzeug-form-dialog.component.scss',
})
export class WerkzeugFormDialogComponent {
  private readonly api = inject(ApiService);
  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject(MatDialogRef<WerkzeugFormDialogComponent>);
  readonly data: WerkzeugFormDialogData = inject(MAT_DIALOG_DATA);

  readonly saving = signal(false);
  readonly isEdit = this.data.werkzeug !== null;

  readonly form = this.fb.nonNullable.group({
    name:        [this.data.werkzeug?.name ?? '',        Validators.required],
    description: [this.data.werkzeug?.description ?? '', Validators.required],
    category:    [this.data.werkzeug?.category ?? '',    Validators.required],
    imageUrl:    [this.data.werkzeug?.imageUrl ?? ''],
    dimensions:  [this.data.werkzeug?.dimensions ?? ''],
  });

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const value = this.form.getRawValue();
    const body = {
      name:        value.name,
      description: value.description,
      category:    value.category,
      imageUrl:    value.imageUrl || null,
      dimensions:  value.dimensions || null,
    };

    const request$ = this.isEdit
      ? this.api.put<Werkzeug>(`/werkzeug/${this.data.werkzeug!.id}`, body)
      : this.api.post<Werkzeug>('/werkzeug', body);

    request$.subscribe({
      next: result => this.dialogRef.close(result),
      error: () => {
        this.snackBar.open('Fehler beim Speichern.', 'OK', { duration: 3000 });
        this.saving.set(false);
      },
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
