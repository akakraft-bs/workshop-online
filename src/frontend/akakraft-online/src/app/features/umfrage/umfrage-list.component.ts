import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import {
  Umfrage,
  UmfrageOption,
  CreateUmfrageDto,
  UpdateUmfrageDto,
  VoteUmfrageDto,
} from '../../models/umfrage.model';
import {
  CreateUmfrageDialogComponent,
  UmfrageDialogData,
} from './create-umfrage-dialog/create-umfrage-dialog.component';

type StatusFilter = 'offen' | 'geschlossen' | 'alle';

@Component({
  selector: 'app-umfrage-list',
  imports: [
    DatePipe,
    MatButtonModule,
    MatButtonToggleModule,
    MatIconModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './umfrage-list.component.html',
  styleUrl: './umfrage-list.component.scss',
})
export class UmfrageListComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly items = signal<Umfrage[]>([]);
  readonly loading = signal(true);
  readonly statusFilter = signal<StatusFilter>('offen');
  readonly pendingVoteId = signal<string | null>(null);

  readonly currentUserId = computed(() => this.auth.currentUser()?.id ?? null);
  readonly isPrivileged = computed(() => this.auth.isAdmin() || this.auth.isVorstand());

  readonly openCount = computed(() =>
    this.items().filter(u => u.status === 'Offen').length
  );

  readonly filteredItems = computed(() => {
    const filter = this.statusFilter();
    const sorted = [...this.items()].sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    );
    if (filter === 'offen') return sorted.filter(u => u.status === 'Offen');
    if (filter === 'geschlossen') return sorted.filter(u => u.status === 'Geschlossen');
    return sorted;
  });

  canEdit(umfrage: Umfrage): boolean {
    return (
      umfrage.createdByUserId === this.currentUserId() ||
      this.isPrivileged()
    );
  }

  canClose(umfrage: Umfrage): boolean {
    return umfrage.status === 'Offen' && this.canEdit(umfrage);
  }

  isSelected(umfrage: Umfrage, option: UmfrageOption): boolean {
    return umfrage.currentUserOptionIds.includes(option.id);
  }

  totalVotes(umfrage: Umfrage): number {
    return umfrage.options.reduce((sum, o) => sum + (o.voteCount ?? 0), 0);
  }

  votePercent(option: UmfrageOption, umfrage: Umfrage): number {
    const total = this.totalVotes(umfrage);
    if (!option.voteCount || total === 0) return 0;
    return Math.round((option.voteCount / total) * 100);
  }

  isDeadlineUrgent(umfrage: Umfrage): boolean {
    if (!umfrage.deadline) return false;
    const diffMs = new Date(umfrage.deadline).getTime() - Date.now();
    return diffMs > 0 && diffMs < 3_600_000; // less than 1 hour
  }

  deadlineLabel(umfrage: Umfrage): string | null {
    if (!umfrage.deadline) return null;
    const d = new Date(umfrage.deadline);
    const now = new Date();
    const diffMs = d.getTime() - now.getTime();
    if (diffMs <= 0) return 'Abgelaufen';
    const diffH = Math.floor(diffMs / 3_600_000);
    const diffM = Math.floor((diffMs % 3_600_000) / 60_000);
    if (diffH > 48) {
      return `Frist: ${d.toLocaleDateString('de-DE', { day: '2-digit', month: '2-digit', year: 'numeric' })} ${d.toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit' })} Uhr`;
    }
    if (diffH > 0) return `Noch ${diffH}h ${diffM}min`;
    return `Noch ${diffM} min`;
  }

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.api.get<Umfrage[]>('/umfrage').subscribe({
      next: data => { this.items.set(data); this.loading.set(false); },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Umfragen konnten nicht geladen werden.', 'OK', { duration: 3000 });
      },
    });
  }

  openCreateDialog(): void {
    this.dialog
      .open(CreateUmfrageDialogComponent, { width: '520px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((result: CreateUmfrageDto | undefined) => {
        if (!result) return;
        this.api.post<Umfrage>('/umfrage', result).subscribe({
          next: created => {
            this.items.update(list => [created, ...list]);
            this.snackBar.open('Umfrage wurde erstellt.', 'OK', { duration: 3000 });
          },
          error: () => this.snackBar.open('Fehler beim Erstellen.', 'OK', { duration: 3000 }),
        });
      });
  }

  openEditDialog(umfrage: Umfrage): void {
    const data: UmfrageDialogData = { umfrage };
    this.dialog
      .open(CreateUmfrageDialogComponent, { data, width: '520px', maxWidth: '95vw' })
      .afterClosed()
      .subscribe((result: UpdateUmfrageDto | undefined) => {
        if (!result) return;
        this.api.put<Umfrage>(`/umfrage/${umfrage.id}`, result).subscribe({
          next: updated => {
            this.items.update(list => list.map(u => u.id === updated.id ? updated : u));
            this.snackBar.open('Umfrage wurde gespeichert.', 'OK', { duration: 3000 });
          },
          error: () => this.snackBar.open('Fehler beim Speichern.', 'OK', { duration: 3000 }),
        });
      });
  }

  deleteUmfrage(umfrage: Umfrage): void {
    if (!confirm(`Umfrage "${umfrage.question}" wirklich löschen?`)) return;
    this.api.delete(`/umfrage/${umfrage.id}`).subscribe({
      next: () => {
        this.items.update(list => list.filter(u => u.id !== umfrage.id));
        this.snackBar.open('Umfrage wurde gelöscht.', 'OK', { duration: 3000 });
      },
      error: () => this.snackBar.open('Fehler beim Löschen.', 'OK', { duration: 3000 }),
    });
  }

  closeUmfrage(umfrage: Umfrage): void {
    if (!confirm(`Umfrage "${umfrage.question}" schließen? Keine weiteren Antworten möglich.`)) return;
    this.api.post<Umfrage>(`/umfrage/${umfrage.id}/close`, {}).subscribe({
      next: updated => {
        this.items.update(list => list.map(u => u.id === updated.id ? updated : u));
        this.snackBar.open('Umfrage wurde geschlossen.', 'OK', { duration: 3000 });
      },
      error: () => this.snackBar.open('Fehler beim Schließen.', 'OK', { duration: 3000 }),
    });
  }

  vote(umfrage: Umfrage, option: UmfrageOption): void {
    if (umfrage.status !== 'Offen') return;
    if (this.pendingVoteId()) return;
    this.pendingVoteId.set(umfrage.id);

    let newOptionIds: string[];
    if (umfrage.isMultipleChoice) {
      // Toggle the option
      const current = umfrage.currentUserOptionIds;
      newOptionIds = current.includes(option.id)
        ? current.filter(id => id !== option.id)
        : [...current, option.id];
    } else {
      // Single choice: toggle or replace
      newOptionIds = umfrage.currentUserOptionIds.includes(option.id) ? [] : [option.id];
    }

    const dto: VoteUmfrageDto = { optionIds: newOptionIds };
    this.api.post<Umfrage>(`/umfrage/${umfrage.id}/vote`, dto).subscribe({
      next: updated => {
        this.items.update(list => list.map(u => u.id === updated.id ? updated : u));
        this.pendingVoteId.set(null);
      },
      error: () => {
        this.snackBar.open('Abstimmung fehlgeschlagen.', 'OK', { duration: 3000 });
        this.pendingVoteId.set(null);
      },
    });
  }
}
