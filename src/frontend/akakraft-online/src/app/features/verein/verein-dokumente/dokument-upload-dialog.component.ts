import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { switchMap } from 'rxjs';
import { ApiService } from '../../../core/api/api.service';
import { DokumentDto, DokumentOrdnerDto } from '../../../models/verein.models';

@Component({
  selector: 'app-dokument-upload-dialog',
  imports: [MatDialogModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <h2 mat-dialog-title>Dokument hochladen – {{ data.name }}</h2>
    <mat-dialog-content>
      <div class="upload-area">
        @if (selectedFile()) {
          <div class="file-selected">
            <mat-icon>description</mat-icon>
            <span>{{ selectedFile()!.name }}</span>
            <span class="file-size">({{ formatSize(selectedFile()!.size) }})</span>
          </div>
        } @else {
          <mat-icon class="upload-icon">upload_file</mat-icon>
          <p>Datei auswählen</p>
        }
        <input #fileInput type="file" (change)="onFileSelected($event)" hidden>
        <button mat-stroked-button type="button" (click)="fileInput.click()">
          {{ selectedFile() ? 'Andere Datei' : 'Datei wählen' }}
        </button>
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close()" [disabled]="saving()">Abbrechen</button>
      <button mat-raised-button color="primary" (click)="upload()" [disabled]="!selectedFile() || saving()">
        @if (saving()) { <mat-spinner diameter="18" /> } @else { Hochladen }
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .upload-area { display: flex; flex-direction: column; align-items: center; gap: 12px; padding: 16px 0; }
    .upload-icon { font-size: 48px; width: 48px; height: 48px; color: var(--mat-sys-on-surface-variant); }
    .file-selected { display: flex; align-items: center; gap: 8px; }
    .file-size { font-size: 12px; color: var(--mat-sys-on-surface-variant); }
  `],
})
export class DokumentUploadDialogComponent {
  private readonly api      = inject(ApiService);
  private readonly snackBar = inject(MatSnackBar);
  readonly dialogRef        = inject(MatDialogRef<DokumentUploadDialogComponent>);
  readonly data: DokumentOrdnerDto = inject(MAT_DIALOG_DATA);

  readonly saving       = signal(false);
  readonly selectedFile = signal<File | null>(null);

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) this.selectedFile.set(file);
  }

  upload(): void {
    const file = this.selectedFile();
    if (!file) return;
    this.saving.set(true);

    const formData = new FormData();
    formData.append('file', file);

    this.api.postFormData<{ url: string }>('/uploads/dokument', formData).pipe(
      switchMap(({ url }) =>
        this.api.post<DokumentDto>('/verein/dokumente', {
          folderId:      this.data.id,
          fileName:      file.name,
          fileUrl:       url,
          fileSizeBytes: file.size,
        })
      )
    ).subscribe({
      next: dok => this.dialogRef.close(dok),
      error: () => { this.snackBar.open('Hochladen fehlgeschlagen.', 'OK', { duration: 3000 }); this.saving.set(false); },
    });
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }
}
