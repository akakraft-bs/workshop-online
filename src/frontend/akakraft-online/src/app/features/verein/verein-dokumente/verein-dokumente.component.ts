import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatExpansionModule } from '@angular/material/expansion';
import { ApiService } from '../../../core/api/api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { DokumentDto, DokumentOrdnerDto } from '../../../models/verein.models';
import { OrdnerDialogComponent } from './ordner-dialog.component';
import { DokumentUploadDialogComponent } from './dokument-upload-dialog.component';

@Component({
  selector: 'app-verein-dokumente',
  imports: [
    DatePipe, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatTooltipModule, MatExpansionModule,
  ],
  templateUrl: './verein-dokumente.component.html',
  styleUrl: './verein-dokumente.component.scss',
})
export class VereinDokumenteComponent implements OnInit {
  private readonly api      = inject(ApiService);
  private readonly auth     = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog   = inject(MatDialog);

  readonly loading  = signal(true);
  readonly ordner   = signal<DokumentOrdnerDto[]>([]);
  readonly canEdit  = computed(() => this.auth.isAdmin() || this.auth.isVorstand());

  ngOnInit(): void { this.load(); }

  private load(): void {
    this.api.get<DokumentOrdnerDto[]>('/verein/dokumente').subscribe({
      next: data => { this.ordner.set(data); this.loading.set(false); },
      error: () => { this.loading.set(false); this.snackBar.open('Laden fehlgeschlagen.', 'OK', { duration: 3000 }); },
    });
  }

  openCreateOrdner(): void {
    this.dialog
      .open(OrdnerDialogComponent, { width: '400px' })
      .afterClosed()
      .subscribe((created: DokumentOrdnerDto | undefined) => {
        if (created) this.ordner.update(list => [...list, created]);
      });
  }

  deleteOrdner(ordner: DokumentOrdnerDto): void {
    this.api.delete(`/verein/dokumente/ordner/${ordner.id}`).subscribe({
      next: () => this.ordner.update(list => list.filter(o => o.id !== ordner.id)),
      error: () => this.snackBar.open('Löschen fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }

  openUpload(ordner: DokumentOrdnerDto): void {
    this.dialog
      .open(DokumentUploadDialogComponent, { data: ordner, width: '480px' })
      .afterClosed()
      .subscribe((created: DokumentDto | undefined) => {
        if (created) {
          this.ordner.update(list =>
            list.map(o => o.id === ordner.id ? { ...o, dokumente: [...o.dokumente, created] } : o)
          );
        }
      });
  }

  deleteDokument(ordner: DokumentOrdnerDto, dok: DokumentDto): void {
    this.api.delete(`/verein/dokumente/${dok.id}`).subscribe({
      next: () => this.ordner.update(list =>
        list.map(o => o.id === ordner.id
          ? { ...o, dokumente: o.dokumente.filter(d => d.id !== dok.id) }
          : o)
      ),
      error: () => this.snackBar.open('Löschen fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }

  formatSize(bytes?: number | null): string {
    if (!bytes) return '';
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }
}
