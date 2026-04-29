import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { ApiService } from '../../../core/api/api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { ProjektDto } from '../../../models/verein.models';
import { ProjektDialogComponent } from './projekt-dialog.component';

@Component({
  selector: 'app-verein-projekte',
  imports: [
    DatePipe, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatTooltipModule, MatChipsModule,
  ],
  templateUrl: './verein-projekte.component.html',
  styleUrl: './verein-projekte.component.scss',
})
export class VereinProjekteComponent implements OnInit {
  private readonly api      = inject(ApiService);
  private readonly auth     = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog   = inject(MatDialog);

  readonly loading  = signal(true);
  readonly items    = signal<ProjektDto[]>([]);
  readonly canEdit  = computed(() => this.auth.isAdmin() || this.auth.isVorstand());

  ngOnInit(): void { this.load(); }

  private load(): void {
    this.api.get<ProjektDto[]>('/verein/projekte').subscribe({
      next: data => { this.items.set(data); this.loading.set(false); },
      error: () => { this.loading.set(false); this.snackBar.open('Laden fehlgeschlagen.', 'OK', { duration: 3000 }); },
    });
  }

  openCreate(): void {
    this.dialog
      .open(ProjektDialogComponent, { data: null, width: '560px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((created: ProjektDto | undefined) => {
        if (created) this.items.update(list => [created, ...list]);
      });
  }

  openEdit(projekt: ProjektDto): void {
    this.dialog
      .open(ProjektDialogComponent, { data: projekt, width: '560px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((updated: ProjektDto | undefined) => {
        if (updated) this.items.update(list => list.map(p => p.id === updated.id ? updated : p));
      });
  }

  delete(projekt: ProjektDto): void {
    this.api.delete(`/verein/projekte/${projekt.id}`).subscribe({
      next: () => this.items.update(list => list.filter(p => p.id !== projekt.id)),
      error: () => this.snackBar.open('Löschen fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }

  statusColor(status: string): string {
    switch (status) {
      case 'Gestartet':    return 'primary';
      case 'Abgeschlossen': return 'accent';
      default:             return '';
    }
  }

  statusIcon(status: string): string {
    switch (status) {
      case 'Gestartet':    return 'play_circle';
      case 'Abgeschlossen': return 'check_circle';
      default:             return 'schedule';
    }
  }
}
