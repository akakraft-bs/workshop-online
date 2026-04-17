import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { switchMap, of, Observable } from 'rxjs';
import { ApiService } from '../../../core/api/api.service';
import { MangelKategorie } from '../../../models/mangel.model';
import { Mangel } from '../../../models/mangel.model';

@Component({
  selector: 'app-create-mangel-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule,
  ],
  templateUrl: './create-mangel-dialog.component.html',
  styleUrl: './create-mangel-dialog.component.scss',
})
export class CreateMangelDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject(MatDialogRef<CreateMangelDialogComponent>);

  readonly kategorien: MangelKategorie[] = ['Halle', 'Werkzeug', 'Sonstiges'];
  readonly saving = signal(false);
  readonly selectedFile = signal<File | null>(null);
  readonly previewUrl = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.required, Validators.maxLength(2000)]],
    kategorie: ['' as MangelKategorie, Validators.required],
  });

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    const allowed = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];
    if (!allowed.includes(file.type)) {
      this.snackBar.open('Ungültiger Dateityp. Erlaubt: JPEG, PNG, WebP, GIF.', 'OK', { duration: 4000 });
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      this.snackBar.open('Datei zu groß. Maximal 5 MB erlaubt.', 'OK', { duration: 4000 });
      return;
    }

    this.selectedFile.set(file);
    this.previewUrl.set(URL.createObjectURL(file));
  }

  clearImage(): void {
    this.selectedFile.set(null);
    this.previewUrl.set(null);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const value = this.form.getRawValue();
    const file = this.selectedFile();

    const upload$: Observable<{ url: string } | null> = file
      ? (() => {
          const formData = new FormData();
          formData.append('file', file);
          return this.api.postFormData<{ url: string }>('/uploads/mangel', formData);
        })()
      : of(null);

    upload$.pipe(
      switchMap((result: { url: string } | null) => {
        const body = { ...value, imageUrl: result?.url ?? null };
        return this.api.post<Mangel>('/mangel', body);
      })
    ).subscribe({
      next: created => this.dialogRef.close(created),
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
