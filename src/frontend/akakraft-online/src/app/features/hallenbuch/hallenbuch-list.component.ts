import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { HallenbuchEintrag } from '../../models/hallenbuch.model';
import {
  HallenbuchDialogComponent,
  HallenbuchDialogData,
  HallenbuchDialogResult,
} from './hallenbuch-dialog/hallenbuch-dialog.component';
import { Mangel } from '../../models/mangel.model';
import { HallenbuchStatistikDialogComponent } from './hallenbuch-statistik-dialog/hallenbuch-statistik-dialog.component';

@Component({
  selector: 'app-hallenbuch-list',
  imports: [
    DatePipe,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatTooltipModule, MatChipsModule,
  ],
  templateUrl: './hallenbuch-list.component.html',
  styleUrl: './hallenbuch-list.component.scss',
})
export class HallenbuchListComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly items = signal<HallenbuchEintrag[]>([]);
  readonly loading = signal(true);

  readonly currentUserId = computed(() => this.auth.currentUser()?.id ?? null);
  readonly isPrivileged = computed(() => this.auth.isAdmin() || this.auth.isVorstand());

  canEdit(eintrag: HallenbuchEintrag): boolean {
    if (this.isPrivileged()) return true;
    if (eintrag.userId !== this.currentUserId()) return false;
    const age = Date.now() - new Date(eintrag.createdAt).getTime();
    return age < 7 * 24 * 60 * 60 * 1000;
  }

  /** Duration in hours formatted */
  duration(eintrag: HallenbuchEintrag): string {
    const ms = new Date(eintrag.end).getTime() - new Date(eintrag.start).getTime();
    const h = Math.floor(ms / 3600000);
    const m = Math.floor((ms % 3600000) / 60000);
    return m > 0 ? `${h}h ${m}min` : `${h}h`;
  }

  gastschraubenLabel(eintrag: HallenbuchEintrag): string {
    if (!eintrag.hatGastgeschraubt) return '';
    const art = eintrag.gastschraubenArt === 'KastenPremiumbier'
      ? 'Kasten Premiumbier'
      : '20€ PayPal';
    const paid = eintrag.gastschraubenBezahlt ? ' · bezahlt' : ' · ausstehend';
    return art + paid;
  }

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.api.get<HallenbuchEintrag[]>('/hallenbuch').subscribe({
      next: data => { this.items.set(this.sortByStart(data)); this.loading.set(false); },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Hallenbuch konnte nicht geladen werden.', 'OK', { duration: 3000 });
      },
    });
  }

  openCreateDialog(): void {
    this.dialog
      .open(HallenbuchDialogComponent, { width: '520px' })
      .afterClosed()
      .subscribe((result: HallenbuchDialogResult | undefined) => {
        if (!result) return;
        this.api.post<HallenbuchEintrag>('/hallenbuch', result.hallenbuch).subscribe({
          next: created => {
            this.items.update(list => this.sortByStart([created, ...list]));
            if (result.mangel) {
              this.api.post<Mangel>('/mangel', result.mangel).subscribe({
                next: () => {
                  this.snackBar.open('Eintrag und Mangel wurden gespeichert.', 'OK', { duration: 3000 });
                },
                error: () => {
                  this.snackBar.open('Eintrag gespeichert, Mangel konnte nicht gemeldet werden.', 'OK', { duration: 4000 });
                },
              });
            } else {
              this.snackBar.open('Eintrag wurde gespeichert.', 'OK', { duration: 3000 });
            }
          },
          error: (err) => {
            const msg = err?.error ?? 'Fehler beim Speichern.';
            this.snackBar.open(typeof msg === 'string' ? msg : 'Fehler beim Speichern.', 'OK', { duration: 4000 });
          },
        });
      });
  }

  openEditDialog(eintrag: HallenbuchEintrag): void {
    const data: HallenbuchDialogData = { eintrag };
    this.dialog
      .open(HallenbuchDialogComponent, { data, width: '520px' })
      .afterClosed()
      .subscribe((result: HallenbuchDialogResult | undefined) => {
        if (!result) return;
        this.api.put<HallenbuchEintrag>(`/hallenbuch/${eintrag.id}`, result.hallenbuch).subscribe({
          next: updated => {
            this.items.update(list => this.sortByStart(list.map(e => e.id === updated.id ? updated : e)));
            this.snackBar.open('Eintrag wurde aktualisiert.', 'OK', { duration: 3000 });
          },
          error: () => this.snackBar.open('Fehler beim Speichern.', 'OK', { duration: 3000 }),
        });
      });
  }

  deleteEintrag(eintrag: HallenbuchEintrag): void {
    if (!confirm(`Eintrag von ${eintrag.userName} vom ${new Date(eintrag.start).toLocaleDateString('de-DE')} löschen?`)) return;
    this.api.delete<void>(`/hallenbuch/${eintrag.id}`).subscribe({
      next: () => {
        this.items.update(list => list.filter(e => e.id !== eintrag.id));
        this.snackBar.open('Eintrag wurde gelöscht.', 'OK', { duration: 3000 });
      },
      error: () => this.snackBar.open('Fehler beim Löschen.', 'OK', { duration: 3000 }),
    });
  }

  openStatistikDialog(): void {
    this.dialog.open(HallenbuchStatistikDialogComponent, { width: '360px' });
  }

  private sortByStart(list: HallenbuchEintrag[]): HallenbuchEintrag[] {
    return [...list].sort((a, b) => new Date(b.start).getTime() - new Date(a.start).getTime());
  }
}
