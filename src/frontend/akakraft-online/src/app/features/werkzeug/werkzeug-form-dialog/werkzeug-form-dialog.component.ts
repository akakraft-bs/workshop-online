import { Component, HostListener, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, switchMap, of, map, startWith } from 'rxjs';
import { AsyncPipe } from '@angular/common';
import { ApiService } from '../../../core/api/api.service';
import { Werkzeug } from '../../../models/werkzeug.model';
import { DokumentDto, DokumentOrdnerDto } from '../../../models/verein.models';

export interface WerkzeugFormDialogData {
  werkzeug: Werkzeug | null;
}

@Component({
  selector: 'app-werkzeug-form-dialog',
  imports: [
    ReactiveFormsModule, AsyncPipe,
    MatDialogModule, MatButtonModule, MatFormFieldModule,
    MatInputModule, MatProgressSpinnerModule, MatIconModule,
    MatDividerModule, MatTooltipModule,
    MatAutocompleteModule, MatSelectModule,
  ],
  templateUrl: './werkzeug-form-dialog.component.html',
  styleUrl: './werkzeug-form-dialog.component.scss',
})
export class WerkzeugFormDialogComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject(MatDialogRef<WerkzeugFormDialogComponent>);
  readonly data: WerkzeugFormDialogData = inject(MAT_DIALOG_DATA);

  readonly saving = signal(false);
  readonly isEdit = this.data.werkzeug !== null;

  readonly selectedFile = signal<File | null>(null);
  readonly previewUrl    = signal<string | null>(this.data.werkzeug?.imageUrl ?? null);
  private thumbnailUrl   = this.data.werkzeug?.thumbnailUrl ?? null;

  // Anleitung
  readonly anleitungDokumente  = signal<DokumentDto[]>([]);
  readonly anleitungFolderId   = signal<string | null>(null);
  readonly anleitungSelected   = signal<DokumentDto | null>(null);
  readonly anleitungUploading  = signal(false);

  allCategories: string[] = [];
  allStorageLocations: { name: string; color?: string }[] = [];
  filteredCategories$!: Observable<string[]>;
  filteredStorageLocations$!: Observable<string[]>;

  readonly form = this.fb.nonNullable.group({
    name:            [this.data.werkzeug?.name ?? '',            Validators.required],
    description:     [this.data.werkzeug?.description ?? '',     Validators.required],
    category:        [this.data.werkzeug?.category ?? '',        Validators.required],
    dimensions:      [this.data.werkzeug?.dimensions ?? ''],
    storageLocation: [this.data.werkzeug?.storageLocation ?? ''],
  });

  @HostListener('paste', ['$event'])
  onPaste(event: ClipboardEvent): void {
    const items = event.clipboardData?.items;
    if (!items) return;
    for (const clipItem of Array.from(items)) {
      if (clipItem.type.startsWith('image/')) {
        const file = clipItem.getAsFile();
        if (file) { this.setFile(file); event.preventDefault(); }
        return;
      }
    }
  }

  ngOnInit(): void {
    this.api.get<string[]>('/werkzeug/categories').subscribe(cats => {
      this.allCategories = cats;
    });
    this.api.get<{ name: string; color?: string }[]>('/storage-locations').subscribe(locs => {
      this.allStorageLocations = locs;
    });

    // Anleitungen-Ordner laden
    this.api.get<DokumentOrdnerDto[]>('/verein/dokumente').subscribe(ordner => {
      const anleitungen = ordner.find(o => o.name.toLowerCase() === 'anleitungen');
      if (anleitungen) {
        this.anleitungFolderId.set(anleitungen.id);
        this.anleitungDokumente.set(anleitungen.dokumente);
        // Vorhandene Anleitung vorauswählen
        const existing = anleitungen.dokumente.find(
          d => d.id === this.data.werkzeug?.anleitungDokumentId
        );
        if (existing) this.anleitungSelected.set(existing);
      }
      // Fallback: Anleitung ist bekannt aber nicht im Ordner (z. B. Ordner fehlt)
      if (!this.anleitungSelected() && this.data.werkzeug?.anleitungDokumentId && this.data.werkzeug.anleitungFileName) {
        this.anleitungSelected.set({
          id: this.data.werkzeug.anleitungDokumentId,
          folderId: '',
          fileName: this.data.werkzeug.anleitungFileName,
          fileUrl: this.data.werkzeug.anleitungFileUrl ?? '',
          uploadedByName: '',
          uploadedAt: '',
          fileSizeBytes: null,
        });
      }
    });

    this.filteredCategories$ = this.form.controls.category.valueChanges.pipe(
      startWith(this.form.controls.category.value),
      map(value => {
        const filter = value.toLowerCase();
        return this.allCategories.filter(c => c.toLowerCase().includes(filter));
      }),
    );

    this.filteredStorageLocations$ = this.form.controls.storageLocation.valueChanges.pipe(
      startWith(this.form.controls.storageLocation.value),
      map(value => {
        const filter = (value ?? '').toLowerCase();
        return this.allStorageLocations
          .filter(l => l.name.toLowerCase().includes(filter))
          .map(l => l.name);
      }),
    );
  }

  onAnleitungSelect(dokId: string | null): void {
    if (!dokId) { this.anleitungSelected.set(null); return; }
    const dok = this.anleitungDokumente().find(d => d.id === dokId) ?? null;
    this.anleitungSelected.set(dok);
  }

  clearAnleitung(): void {
    this.anleitungSelected.set(null);
  }

  onAnleitungFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    this.anleitungUploading.set(true);
    const formData = new FormData();
    formData.append('file', file);

    this.api.postFormData<{ url: string }>('/uploads/dokument', formData).pipe(
      switchMap(upload => {
        const folderId = this.anleitungFolderId();
        const folder$ = folderId
          ? of({ id: folderId } as { id: string })
          : this.api.post<DokumentOrdnerDto>('/verein/dokumente/ordner', { name: 'Anleitungen' });

        return folder$.pipe(
          switchMap(folder => {
            if (!this.anleitungFolderId()) this.anleitungFolderId.set(folder.id);
            return this.api.post<DokumentDto>('/verein/dokumente', {
              folderId: folder.id,
              fileName: file.name,
              fileUrl: upload.url,
              fileSizeBytes: file.size,
            });
          })
        );
      })
    ).subscribe({
      next: dok => {
        this.anleitungSelected.set(dok);
        this.anleitungDokumente.update(list => [...list, dok]);
        this.anleitungUploading.set(false);
      },
      error: () => {
        this.anleitungUploading.set(false);
        this.snackBar.open('Fehler beim Hochladen der Anleitung.', 'OK', { duration: 3000 });
      },
    });
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
    this.thumbnailUrl = null;
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

    const upload$: Observable<{ imageUrl: string; thumbnailUrl: string } | null> = file
      ? (() => {
          const formData = new FormData();
          formData.append('file', file);
          return this.api.postFormData<{ imageUrl: string; thumbnailUrl: string }>('/uploads/werkzeug', formData);
        })()
      : of(null);

    upload$.pipe(
      switchMap((uploadResult: { imageUrl: string; thumbnailUrl: string } | null) => {
        const imageUrl     = uploadResult?.imageUrl    ?? existingImageUrl;
        const thumbnailUrl = uploadResult?.thumbnailUrl ?? this.thumbnailUrl;

        const body = {
          name:                 value.name,
          description:          value.description,
          category:             value.category,
          imageUrl,
          thumbnailUrl,
          dimensions:           value.dimensions || null,
          storageLocation:      value.storageLocation || null,
          anleitungDokumentId:  this.anleitungSelected()?.id ?? null,
        };

        return this.isEdit
          ? this.api.put<Werkzeug>(`/werkzeug/${this.data.werkzeug!.id}`, body)
          : this.api.post<Werkzeug>('/werkzeug', body);
      })
    ).subscribe({
      next: (result: Werkzeug) => this.dialogRef.close(result),
      error: () => {
        this.snackBar.open('Fehler beim Speichern.', 'OK', { duration: 3000 });
        this.saving.set(false);
      },
    });
  }

  cancel(): void {
    this.dialogRef.close();
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
