import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { Wunsch, WunschStatus } from '../../models/wunsch.model';
import { CreateWunschDialogComponent, WunschDialogData } from './create-wunsch-dialog/create-wunsch-dialog.component';
import {
  CloseWunschDialogComponent,
  CloseWunschDialogData,
} from './close-wunsch-dialog/close-wunsch-dialog.component';

type StatusFilter = 'offen' | 'abgeschlossen' | 'alle';

@Component({
  selector: 'app-wunsch-list',
  imports: [
    DatePipe,
    MatButtonModule, MatButtonToggleModule,
    MatIconModule, MatProgressSpinnerModule, MatTooltipModule,
  ],
  templateUrl: './wunsch-list.component.html',
  styleUrl: './wunsch-list.component.scss',
})
export class WunschListComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly items = signal<Wunsch[]>([]);
  readonly loading = signal(true);
  readonly statusFilter = signal<StatusFilter>('offen');
  readonly pendingVoteId = signal<string | null>(null);

  readonly canClose = computed(() => this.auth.isAdmin() || this.auth.isVorstand());
  readonly currentUserId = computed(() => this.auth.currentUser()?.id ?? null);

  canEdit(wunsch: Wunsch): boolean {
    return wunsch.createdByUserId === this.currentUserId()
      || this.auth.isAdmin()
      || this.auth.isVorstand();
  }

  readonly openCount = computed(() =>
    this.items().filter(w => w.status === 'Offen').length
  );

  readonly filteredItems = computed(() => {
    const filter = this.statusFilter();
    const sorted = [...this.items()].sort((a, b) => {
      // Within same status bucket: sort by net votes desc, then date desc
      const netA = a.upVotes - a.downVotes;
      const netB = b.upVotes - b.downVotes;
      if (netA !== netB) return netB - netA;
      return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
    });

    if (filter === 'offen') return sorted.filter(w => w.status === 'Offen');
    if (filter === 'abgeschlossen') return sorted.filter(w => w.status !== 'Offen');
    return sorted;
  });

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.api.get<Wunsch[]>('/wunsch').subscribe({
      next: data => { this.items.set(data); this.loading.set(false); },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Wunschliste konnte nicht geladen werden.', 'OK', { duration: 3000 });
      },
    });
  }

  openCreateDialog(): void {
    this.dialog
      .open(CreateWunschDialogComponent, { width: '480px' })
      .afterClosed()
      .subscribe((result: { title: string; description: string; link: string | null } | undefined) => {
        if (!result) return;
        this.api.post<Wunsch>('/wunsch', result).subscribe({
          next: created => {
            this.items.update(list => [created, ...list]);
            this.snackBar.open('Vorschlag wurde eingereicht.', 'OK', { duration: 3000 });
          },
          error: () => this.snackBar.open('Fehler beim Einreichen.', 'OK', { duration: 3000 }),
        });
      });
  }

  vote(wunsch: Wunsch, isUpvote: boolean): void {
    if (this.pendingVoteId()) return;
    this.pendingVoteId.set(wunsch.id);

    this.api.post<Wunsch>(`/wunsch/${wunsch.id}/vote`, { isUpvote }).subscribe({
      next: updated => {
        this.items.update(list => list.map(w => w.id === updated.id ? updated : w));
        this.pendingVoteId.set(null);
      },
      error: () => {
        this.snackBar.open('Abstimmung fehlgeschlagen.', 'OK', { duration: 3000 });
        this.pendingVoteId.set(null);
      },
    });
  }

  openEditDialog(wunsch: Wunsch): void {
    const data: WunschDialogData = { wunsch };
    this.dialog
      .open(CreateWunschDialogComponent, { data, width: '480px' })
      .afterClosed()
      .subscribe((result: { title: string; description: string; link: string | null } | undefined) => {
        if (!result) return;
        this.api.put<Wunsch>(`/wunsch/${wunsch.id}`, result).subscribe({
          next: updated => {
            this.items.update(list => list.map(w => w.id === updated.id ? updated : w));
            this.snackBar.open('Vorschlag wurde gespeichert.', 'OK', { duration: 3000 });
          },
          error: () => this.snackBar.open('Fehler beim Speichern.', 'OK', { duration: 3000 }),
        });
      });
  }

  openCloseDialog(wunsch: Wunsch): void {
    const data: CloseWunschDialogData = { wunsch };
    this.dialog
      .open(CloseWunschDialogComponent, { data, width: '420px' })
      .afterClosed()
      .subscribe((result: { status: WunschStatus; closeNote: string | null } | undefined) => {
        if (!result) return;
        this.api.post<Wunsch>(`/wunsch/${wunsch.id}/close`, result).subscribe({
          next: updated => {
            this.items.update(list => list.map(w => w.id === updated.id ? updated : w));
            this.snackBar.open('Vorschlag wurde abgeschlossen.', 'OK', { duration: 3000 });
          },
          error: () => this.snackBar.open('Fehler beim Abschließen.', 'OK', { duration: 3000 }),
        });
      });
  }

  statusIcon(status: WunschStatus): string {
    switch (status) {
      case 'Angeschafft': return 'check_circle';
      case 'Abgelehnt': return 'cancel';
      default: return 'pending';
    }
  }

  statusLabel(status: WunschStatus): string {
    switch (status) {
      case 'Angeschafft': return 'Angeschafft';
      case 'Abgelehnt': return 'Abgelehnt';
      default: return 'Offen';
    }
  }
}
