import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, switchMap, of, map } from 'rxjs';
import { ApiService } from '../../../core/api/api.service';
import { Mangel, MangelKategorie, MangelStatus } from '../../../models/mangel.model';

export interface ResolveMangelDialogData {
  mangel: Mangel;
  canEditContent: boolean;
  canChangeStatus: boolean;
}

interface StatusOption {
  value: MangelStatus;
  label: string;
}

@Component({
  selector: 'app-resolve-mangel-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatButtonToggleModule, MatSelectModule,
    MatIconModule, MatProgressSpinnerModule,
  ],
  templateUrl: './resolve-mangel-dialog.component.html',
  styleUrl: './resolve-mangel-dialog.component.scss',
})
export class ResolveMangelDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);
  readonly dialogRef = inject(MatDialogRef<ResolveMangelDialogComponent>);
  readonly data = inject<ResolveMangelDialogData>(MAT_DIALOG_DATA);

  readonly kategorien: MangelKategorie[] = ['Halle', 'Werkzeug', 'Sonstiges'];

  readonly statusOptions: StatusOption[] = [
    { value: 'Offen',            label: 'Offen lassen'    },
    { value: 'Kenntnisgenommen', label: 'Kenntnisgenommen' },
    { value: 'Behoben',          label: 'Behoben'          },
    { value: 'Abgelehnt',        label: 'Abgelehnt'        },
  ];

  readonly saving = signal(false);
  readonly selectedFile = signal<File | null>(null);
  readonly previewUrl = signal<string | null>(this.data.mangel.imageUrl ?? null);

  readonly form = this.fb.nonNullable.group({
    title:       [this.data.mangel.title,       [Validators.required, Validators.maxLength(200)]],
    description: [this.data.mangel.description, [Validators.required, Validators.maxLength(2000)]],
    kategorie:   [this.data.mangel.kategorie,   Validators.required],
    status:      [this.data.mangel.status],
    note:        [''],
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
    const { title, description, kategorie, status, note } = this.form.getRawValue();
    const file = this.selectedFile();

    const upload$: Observable<string | null> = file
      ? (() => {
          const fd = new FormData();
          fd.append('file', file);
          return this.api.postFormData<{ url: string }>('/uploads/mangel', fd).pipe(map(r => r.url));
        })()
      : of(this.previewUrl() === null ? null : this.data.mangel.imageUrl ?? null);

    upload$.pipe(
      switchMap((imageUrl): Observable<Mangel> => {
        if (!this.data.canEditContent) {
          return of(this.data.mangel);
        }
        return this.api.patch<Mangel>(`/mangel/${this.data.mangel.id}`, {
          title, description, kategorie, imageUrl,
        });
      }),
      switchMap((base): Observable<Mangel> => {
        if (!this.data.canChangeStatus) {
          return of(base);
        }
        return this.api.patch<Mangel>(`/mangel/${base.id}/status`, {
          status, note: note || null,
        });
      }),
    ).subscribe({
      next: (updated: Mangel) => this.dialogRef.close(updated),
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
