import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/api/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { HallenbuchEintrag } from '../../models/hallenbuch.model';
import {
  HallenbuchDialogComponent,
  HallenbuchDialogData,
  HallenbuchDialogPrefill,
  HallenbuchDialogResult,
} from './hallenbuch-dialog/hallenbuch-dialog.component';
import { Mangel } from '../../models/mangel.model';
import { HallenbuchStatistikDialogComponent } from './hallenbuch-statistik-dialog/hallenbuch-statistik-dialog.component';
import { CalendarEvent } from '../../models/calendar.model';
import { User } from '../../models/user.model';

function toTimeStr(d: Date): string {
  return `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
}

function roundTo15Min(d: Date): Date {
  const ms = 15 * 60 * 1000;
  return new Date(Math.round(d.getTime() / ms) * ms);
}

interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

@Component({
  selector: 'app-hallenbuch-list',
  imports: [
    DatePipe,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatTooltipModule, MatChipsModule, MatPaginatorModule,
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
  readonly totalItems = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize  = signal(20);

  readonly currentUserId = computed(() => this.auth.currentUser()?.id ?? null);
  readonly isPrivileged = computed(() => this.auth.isPrivileged());

  canEdit(eintrag: HallenbuchEintrag): boolean {
    if (this.isPrivileged()) return true;
    if (eintrag.userId !== this.currentUserId()) return false;
    const age = Date.now() - new Date(eintrag.createdAt).getTime();
    return age < 7 * 24 * 60 * 60 * 1000;
  }

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
    this.load(0);
  }

  onPage(e: PageEvent): void {
    this.pageSize.set(e.pageSize);
    this.load(e.pageIndex);
  }

  private load(page: number): void {
    this.loading.set(true);
    this.pageIndex.set(page);
    const size = this.pageSize();
    this.api.get<PagedResult<HallenbuchEintrag>>(`/hallenbuch?page=${page}&pageSize=${size}`).subscribe({
      next: result => {
        this.items.set(result.items);
        this.totalItems.set(result.total);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Hallenbuch konnte nicht geladen werden.', 'OK', { duration: 3000 });
      },
    });
  }

  openCreateDialog(): void {
    const user = this.auth.currentUser();
    const todayStart = new Date(); todayStart.setHours(0, 0, 0, 0);
    const todayEnd   = new Date(); todayEnd.setHours(23, 59, 59, 999);

    this.api.get<CalendarEvent[]>(
      `/calendar/events?from=${todayStart.toISOString()}&to=${todayEnd.toISOString()}&type=Hallenbelegung`
    ).subscribe({
      next: events => this.openHallenbuchDialog(this.buildPrefill(this.findMyEventToday(events, user))),
      error: ()     => this.openHallenbuchDialog(undefined),
    });
  }

  private findMyEventToday(events: CalendarEvent[], user: User | null): CalendarEvent | null {
    if (!user) return null;
    const myEvents = events.filter(ev =>
      ev.start != null && (
        ev.creatorUserId === user.id ||
        ev.creatorEmail?.toLowerCase() === user.email.toLowerCase()
      )
    );
    if (myEvents.length === 0) return null;
    const now = Date.now();
    return myEvents.reduce((best, ev) => {
      const diff = (e: CalendarEvent) => Math.abs(new Date(e.start!).getTime() - now);
      return diff(ev) < diff(best) ? ev : best;
    });
  }

  private buildPrefill(event: CalendarEvent | null): HallenbuchDialogPrefill | undefined {
    if (!event?.start) return undefined;
    const now = new Date();
    const end = event.end ? new Date(event.end) : null;
    const endDate = end && now > end ? roundTo15Min(now) : (end ?? now);
    return {
      startTime:   toTimeStr(new Date(event.start)),
      endTime:     toTimeStr(endDate),
      description: (event.description || event.title).substring(0, 256),
    };
  }

  private openHallenbuchDialog(prefill: HallenbuchDialogPrefill | undefined): void {
    const data: HallenbuchDialogData = prefill ? { prefill } : {};
    this.dialog
      .open(HallenbuchDialogComponent, { data, width: '520px' })
      .afterClosed()
      .subscribe((result: HallenbuchDialogResult | undefined) => {
        if (!result) return;
        this.api.post<HallenbuchEintrag>('/hallenbuch', result.hallenbuch).subscribe({
          next: () => {
            // Reload page 0 so the new entry (newest first) is visible
            this.load(0);
            if (result.mangel) {
              this.api.post<Mangel>('/mangel', result.mangel).subscribe({
                next: () => this.snackBar.open('Eintrag und Mangel wurden gespeichert.', 'OK', { duration: 3000 }),
                error: () => this.snackBar.open('Eintrag gespeichert, Mangel konnte nicht gemeldet werden.', 'OK', { duration: 4000 }),
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
            this.items.update(list => list.map(e => e.id === updated.id ? updated : e));
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
        this.load(this.pageIndex());
        this.snackBar.open('Eintrag wurde gelöscht.', 'OK', { duration: 3000 });
      },
      error: () => this.snackBar.open('Fehler beim Löschen.', 'OK', { duration: 3000 }),
    });
  }

  openStatistikDialog(): void {
    this.dialog.open(HallenbuchStatistikDialogComponent, { width: '360px' });
  }
}
