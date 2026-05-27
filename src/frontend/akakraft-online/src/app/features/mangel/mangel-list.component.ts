import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TextFieldModule } from '@angular/cdk/text-field';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import {
  Mangel, MangelAnmerkung, MangelKategorie, MangelStatus,
  MANGEL_KATEGORIE_LABELS, MANGEL_STATUS_LABELS,
} from '../../models/mangel.model';
import { CreateMangelDialogComponent } from './create-mangel-dialog/create-mangel-dialog.component';
import {
  ResolveMangelDialogComponent,
  ResolveMangelDialogData,
} from './resolve-mangel-dialog/resolve-mangel-dialog.component';

type StatusFilter = 'aktiv' | 'abgeschlossen' | 'alle';

@Component({
  selector: 'app-mangel-list',
  imports: [
    DatePipe,
    FormsModule,
    TextFieldModule,
    MatButtonModule, MatButtonToggleModule, MatChipsModule,
    MatFormFieldModule, MatInputModule,
    MatIconModule, MatProgressSpinnerModule, MatTooltipModule,
  ],
  templateUrl: './mangel-list.component.html',
  styleUrl: './mangel-list.component.scss',
})
export class MangelListComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly items = signal<Mangel[]>([]);
  readonly loading = signal(true);
  readonly statusFilter = signal<StatusFilter>('aktiv');
  readonly kategorieFilter = signal<MangelKategorie | null>(null);

  readonly canManage = computed(() => this.auth.isPrivileged());
  readonly currentUserId = computed(() => this.auth.currentUser()?.id ?? null);

  // Anmerkungen state
  readonly commentDrafts = signal<Record<string, string>>({});
  readonly editing = signal<{ anmerkungId: string; mangelId: string; text: string } | null>(null);

  canEditTitle(mangel: Mangel): boolean {
    return mangel.createdByUserId === this.currentUserId() || this.canManage();
  }

  readonly KATEGORIE_LABELS = MANGEL_KATEGORIE_LABELS;
  readonly STATUS_LABELS = MANGEL_STATUS_LABELS;

  readonly kategorien: MangelKategorie[] = ['Halle', 'Werkzeug', 'Sonstiges'];

  readonly filteredItems = computed(() => {
    const filter = this.statusFilter();
    const kat = this.kategorieFilter();

    return this.items().filter(m => {
      if (kat && m.kategorie !== kat) return false;
      if (filter === 'aktiv') return m.status === 'Offen' || m.status === 'Kenntnisgenommen';
      if (filter === 'abgeschlossen') return m.status === 'Behoben' || m.status === 'Abgelehnt' || m.status === 'Zurueckgezogen';
      return true;
    });
  });

  readonly openCount = computed(() =>
    this.items().filter(m => m.status === 'Offen').length
  );

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.api.get<Mangel[]>('/mangel').subscribe({
      next: data => { this.items.set(data); this.loading.set(false); },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Mängel konnten nicht geladen werden.', 'OK', { duration: 3000 });
      },
    });
  }

  openCreateDialog(): void {
    this.dialog
      .open(CreateMangelDialogComponent, { width: '480px' })
      .afterClosed()
      .subscribe((created: Mangel | undefined) => {
        if (!created) return;
        this.items.update(list => [created, ...list]);
        this.snackBar.open('Mangel wurde gemeldet.', 'OK', { duration: 3000 });
      });
  }

  openResolveDialog(mangel: Mangel): void {
    const data: ResolveMangelDialogData = {
      mangel,
      canEditContent: this.canEditTitle(mangel),
      canChangeStatus: this.canManage(),
    };
    this.dialog
      .open(ResolveMangelDialogComponent, { data, width: '480px' })
      .afterClosed()
      .subscribe((updated: Mangel | undefined) => {
        if (!updated) return;
        this.items.update(list => list.map(m => m.id === updated.id ? updated : m));
        this.snackBar.open('Mangel wurde aktualisiert.', 'OK', { duration: 3000 });
      });
  }

  zurueckziehen(mangel: Mangel): void {
    this.api.post<Mangel>(`/mangel/${mangel.id}/zurueckziehen`, {}).subscribe({
      next: updated => {
        this.items.update(list => list.map(m => m.id === updated.id ? updated : m));
        this.snackBar.open('Meldung wurde zurückgezogen.', 'OK', { duration: 3000 });
      },
      error: () => this.snackBar.open('Zurückziehen fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }

  isOwn(mangel: Mangel): boolean {
    return mangel.createdByUserId === this.currentUserId();
  }

  canZurueckziehen(mangel: Mangel): boolean {
    return this.isOwn(mangel) && mangel.status === 'Offen';
  }

  canResolve(mangel: Mangel): boolean {
    return mangel.status === 'Offen' || mangel.status === 'Kenntnisgenommen';
  }

  statusColor(status: MangelStatus): string {
    switch (status) {
      case 'Offen': return 'warn';
      case 'Kenntnisgenommen': return 'accent';
      case 'Behoben': return 'primary';
      default: return '';
    }
  }

  statusIcon(status: MangelStatus): string {
    switch (status) {
      case 'Offen': return 'report_problem';
      case 'Kenntnisgenommen': return 'visibility';
      case 'Behoben': return 'check_circle';
      case 'Abgelehnt': return 'cancel';
      case 'Zurueckgezogen': return 'undo';
    }
  }

  kategorieIcon(kat: MangelKategorie): string {
    switch (kat) {
      case 'Halle': return 'warehouse';
      case 'Werkzeug': return 'build';
      case 'Sonstiges': return 'help_outline';
    }
  }

  // ---- Anmerkungen -------------------------------------------------------

  getDraft(mangelId: string): string {
    return this.commentDrafts()[mangelId] ?? '';
  }

  setDraft(mangelId: string, text: string): void {
    this.commentDrafts.update(d => ({ ...d, [mangelId]: text }));
  }

  addComment(mangel: Mangel): void {
    const text = this.getDraft(mangel.id).trim();
    if (!text) return;
    this.api.post<MangelAnmerkung>(`/mangel/${mangel.id}/anmerkungen`, { text }).subscribe({
      next: created => {
        this.items.update(list => list.map(m =>
          m.id === mangel.id ? { ...m, anmerkungen: [...m.anmerkungen, created] } : m
        ));
        this.setDraft(mangel.id, '');
      },
      error: () => this.snackBar.open('Anmerkung konnte nicht gespeichert werden.', 'OK', { duration: 3000 }),
    });
  }

  startEdit(mangel: Mangel, a: MangelAnmerkung): void {
    this.editing.set({ anmerkungId: a.id, mangelId: mangel.id, text: a.text });
  }

  cancelEdit(): void {
    this.editing.set(null);
  }

  setEditText(text: string): void {
    const e = this.editing();
    if (e) this.editing.set({ ...e, text });
  }

  saveEdit(mangel: Mangel): void {
    const e = this.editing();
    if (!e || !e.text.trim()) return;
    this.api.put<MangelAnmerkung>(`/mangel/${mangel.id}/anmerkungen/${e.anmerkungId}`, { text: e.text.trim() }).subscribe({
      next: updated => {
        this.items.update(list => list.map(m =>
          m.id === mangel.id
            ? { ...m, anmerkungen: m.anmerkungen.map(a => a.id === updated.id ? updated : a) }
            : m
        ));
        this.editing.set(null);
      },
      error: () => this.snackBar.open('Bearbeitung fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }

  deleteComment(mangel: Mangel, a: MangelAnmerkung): void {
    if (!confirm('Anmerkung löschen?')) return;
    this.api.delete<void>(`/mangel/${mangel.id}/anmerkungen/${a.id}`).subscribe({
      next: () => {
        this.items.update(list => list.map(m =>
          m.id === mangel.id
            ? { ...m, anmerkungen: m.anmerkungen.filter(x => x.id !== a.id) }
            : m
        ));
      },
      error: () => this.snackBar.open('Löschen fehlgeschlagen.', 'OK', { duration: 3000 }),
    });
  }

  isOwnAnmerkung(a: MangelAnmerkung): boolean {
    return a.createdByUserId === this.currentUserId();
  }

  isEditing(a: MangelAnmerkung): boolean {
    return this.editing()?.anmerkungId === a.id;
  }
}
