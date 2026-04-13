import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, switchMap, of } from 'rxjs';
import { ApiService } from '../../../core/api/api.service';
import { Verbrauchsmaterial } from '../../../models/verbrauchsmaterial.model';

type ImageMode = 'url' | 'upload';

@Component({
  selector: 'app-verbrauchsmaterial-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule, MatButtonModule, MatFormFieldModule,
    MatInputModule, MatProgressSpinnerModule, MatIconModule,
    MatButtonToggleModule,
  ],
  templateUrl: './verbrauchsmaterial-form-dialog.component.html',
  styleUrl: './verbrauchsmaterial-form-dialog.component.scss',
})
export class VerbrauchsmaterialFormDialogComponent {
  private readonly api = inject(ApiService);
  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject(MatDialogRef<VerbrauchsmaterialFormDialogComponent>);

  readonly saving = signal(false);
  readonly imageMode = signal<ImageMode>('url');
  readonly selectedFile = signal<File | null>(null);
  readonly previewUrl = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name:        ['', Validators.required],
    description: ['', Validators.required],
    category:    ['', Validators.required],
    unit:        ['', Validators.required],
    quantity:    [0, [Validators.required, Validators.min(0)]],
    minQuantity: [null as number | null],
    imageUrl:    [''],
  });

  setImageMode(mode: ImageMode): void {
    this.imageMode.set(mode);
    if (mode === 'url') {
      this.selectedFile.set(null);
      this.previewUrl.set(this.form.controls.imageUrl.value || null);
    }
  }

  onUrlInput(): void {
    if (this.imageMode() === 'url') {
      this.previewUrl.set(this.form.controls.imageUrl.value || null);
    }
  }

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    const allowedTypes = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];
    if (!allowedTypes.includes(file.type)) {
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
    this.form.controls.imageUrl.setValue('');
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const value = this.form.getRawValue();
    const file = this.selectedFile();

    const upload$: Observable<{ url: string } | null> =
      this.imageMode() === 'upload' && file
        ? (() => {
            const formData = new FormData();
            formData.append('file', file);
            return this.api.postFormData<{ url: string }>('/uploads/verbrauchsmaterial', formData);
          })()
        : of(null);

    upload$.pipe(
      switchMap((uploadResult: { url: string } | null) => {
        const imageUrl = uploadResult?.url
          ?? (this.imageMode() === 'url' ? (value.imageUrl || null) : null);

        return this.api.post<Verbrauchsmaterial>('/verbrauchsmaterial', {
          name:        value.name,
          description: value.description,
          category:    value.category,
          unit:        value.unit,
          quantity:    value.quantity,
          minQuantity: value.minQuantity ?? null,
          imageUrl,
        });
      })
    ).subscribe({
      next: (result: Verbrauchsmaterial) => this.dialogRef.close(result),
      error: () => {
        this.snackBar.open('Fehler beim Speichern.', 'OK', { duration: 3000 });
        this.saving.set(false);
      },
    });
  }
}
