import { Component, HostListener, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, switchMap, of, map, startWith } from 'rxjs';
import { AsyncPipe } from '@angular/common';
import { ApiService } from '../../../core/api/api.service';
import { Verbrauchsmaterial } from '../../../models/verbrauchsmaterial.model';

export interface VerbrauchsmaterialFormDialogData {
  item: Verbrauchsmaterial | null;
}

@Component({
  selector: 'app-verbrauchsmaterial-form-dialog',
  imports: [
    ReactiveFormsModule, AsyncPipe,
    MatDialogModule, MatButtonModule, MatFormFieldModule,
    MatInputModule, MatProgressSpinnerModule, MatIconModule,
    MatAutocompleteModule,
  ],
  templateUrl: './verbrauchsmaterial-form-dialog.component.html',
  styleUrl: './verbrauchsmaterial-form-dialog.component.scss',
})
export class VerbrauchsmaterialFormDialogComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject(MatDialogRef<VerbrauchsmaterialFormDialogComponent>);
  readonly data: VerbrauchsmaterialFormDialogData = inject(MAT_DIALOG_DATA);

  readonly saving = signal(false);
  readonly isEdit = this.data?.item !== null && this.data?.item !== undefined;
  readonly selectedFile = signal<File | null>(null);
  readonly previewUrl = signal<string | null>(this.data?.item?.imageUrl ?? null);

  allCategories: string[] = [];
  allUnits: string[] = [];
  filteredCategories$!: Observable<string[]>;
  filteredUnits$!: Observable<string[]>;

  readonly form = this.fb.nonNullable.group({
    name:        [this.data?.item?.name        ?? '', Validators.required],
    description: [this.data?.item?.description ?? '', Validators.required],
    category:    [this.data?.item?.category    ?? '', Validators.required],
    unit:        [this.data?.item?.unit        ?? '', Validators.required],
    quantity:    [this.data?.item?.quantity    ?? 0,  [Validators.required, Validators.min(0)]],
    minQuantity: [this.data?.item?.minQuantity ?? null as number | null],
  });

  @HostListener('paste', ['$event'])
  onPaste(event: ClipboardEvent): void {
    const items = event.clipboardData?.items;
    if (!items) return;
    for (let i = 0; i < items.length; i++) {
      if (items[i].type.startsWith('image/')) {
        const file = items[i].getAsFile();
        if (file) { this.setFile(file); event.preventDefault(); }
        return;
      }
    }
  }

  ngOnInit(): void {
    this.api.get<string[]>('/verbrauchsmaterial/categories').subscribe(cats => {
      this.allCategories = cats;
    });
    this.api.get<string[]>('/verbrauchsmaterial/units').subscribe(units => {
      this.allUnits = units;
    });

    this.filteredCategories$ = this.form.controls.category.valueChanges.pipe(
      startWith(this.form.controls.category.value),
      map(value => {
        const filter = value.toLowerCase();
        return this.allCategories.filter(c => c.toLowerCase().includes(filter));
      }),
    );

    this.filteredUnits$ = this.form.controls.unit.valueChanges.pipe(
      startWith(this.form.controls.unit.value),
      map(value => {
        const filter = value.toLowerCase();
        return this.allUnits.filter(u => u.toLowerCase().includes(filter));
      }),
    );
  }

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) this.setFile(file);
  }

  async pasteFromClipboard(): Promise<void> {
    if (!navigator.clipboard?.read) {
      this.snackBar.open('Bitte Bild mit ⌘+V / Strg+V einfügen.', 'OK', { duration: 3000 });
      return;
    }
    try {
      const items = await navigator.clipboard.read();
      for (const item of items) {
        const imageType = item.types.find(t => t.startsWith('image/'));
        if (imageType) {
          const blob = await item.getType(imageType);
          const ext = imageType.split('/')[1] ?? 'png';
          this.setFile(new File([blob], `clipboard.${ext}`, { type: imageType }));
          return;
        }
      }
      this.snackBar.open('Kein Bild in der Zwischenablage.', 'OK', { duration: 3000 });
    } catch {
      this.snackBar.open('Kein Zugriff – bitte ⌘+V / Strg+V nutzen.', 'OK', { duration: 3000 });
    }
  }

  clearImage(): void {
    this.selectedFile.set(null);
    this.previewUrl.set(null);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const value = this.form.getRawValue();
    const file = this.selectedFile();
    const existingImageUrl = this.previewUrl();

    const upload$: Observable<{ url: string } | null> = file
      ? (() => {
          const formData = new FormData();
          formData.append('file', file);
          return this.api.postFormData<{ url: string }>('/uploads/verbrauchsmaterial', formData);
        })()
      : of(null);

    upload$.pipe(
      switchMap((uploadResult: { url: string } | null) => {
        const imageUrl = uploadResult?.url ?? existingImageUrl;

        const body = {
          name:        value.name,
          description: value.description,
          category:    value.category,
          unit:        value.unit,
          quantity:    value.quantity,
          minQuantity: value.minQuantity ?? null,
          imageUrl,
        };

        return this.isEdit
          ? this.api.put<Verbrauchsmaterial>(`/verbrauchsmaterial/${this.data.item!.id}`, body)
          : this.api.post<Verbrauchsmaterial>('/verbrauchsmaterial', body);
      })
    ).subscribe({
      next: (result: Verbrauchsmaterial) => this.dialogRef.close(result),
      error: () => {
        this.snackBar.open('Fehler beim Speichern.', 'OK', { duration: 3000 });
        this.saving.set(false);
      },
    });
  }

  private setFile(file: File): void {
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
}
