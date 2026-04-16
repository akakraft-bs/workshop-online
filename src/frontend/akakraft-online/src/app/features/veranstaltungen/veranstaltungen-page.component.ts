import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../core/auth/auth.service';
import { CalendarService } from '../../core/calendar/calendar.service';
import { CalendarConfig, CalendarEvent } from '../../models/calendar.model';
import { Role } from '../../models/user.model';
import {
  EventFormDialogComponent,
  EventFormDialogResult,
} from '../kalender/event-form-dialog/event-form-dialog.component';

export interface MonthGroup {
  key: string;        // "2026-04"
  label: string;      // "April 2026"
  isPast: boolean;
  events: CalendarEvent[];
}

@Component({
  selector: 'app-veranstaltungen-page',
  imports: [
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './veranstaltungen-page.component.html',
  styleUrl: './veranstaltungen-page.component.scss',
})
export class VeranstaltungenPageComponent implements OnInit {
  private readonly calendarService = inject(CalendarService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);

  readonly loading = signal(false);
  readonly configs = signal<CalendarConfig[]>([]);
  readonly events = signal<CalendarEvent[]>([]);
  readonly showPast = signal(false);

  readonly writableCalendarIds = computed(() => {
    const user = this.auth.currentUser();
    if (!user) return [];
    const userRoles = new Set(user.roles as string[]);

    if (
      userRoles.has(Role.Admin) ||
      userRoles.has(Role.Chairman) ||
      userRoles.has(Role.ViceChairman)
    ) {
      return this.configs().map(c => c.googleCalendarId);
    }

    return this.configs()
      .filter(c =>
        c.writeRoles.length > 0 && c.writeRoles.some(r => userRoles.has(r))
      )
      .map(c => c.googleCalendarId);
  });

  readonly canWrite = computed(() => this.writableCalendarIds().length > 0);

  readonly monthGroups = computed<MonthGroup[]>(() => {
    const now = new Date();
    const todayKey = monthKey(now);

    const filtered = this.events()
      .slice()
      .sort((a, b) => {
        const da = a.start ? new Date(a.start).getTime() : 0;
        const db = b.start ? new Date(b.start).getTime() : 0;
        return da - db;
      });

    const groupMap = new Map<string, CalendarEvent[]>();
    for (const ev of filtered) {
      const key = ev.start ? monthKey(new Date(ev.start)) : 'unknown';
      if (!groupMap.has(key)) groupMap.set(key, []);
      groupMap.get(key)!.push(ev);
    }

    const groups: MonthGroup[] = [];
    for (const [key, evs] of groupMap.entries()) {
      groups.push({
        key,
        label: formatMonthLabel(key),
        isPast: key < todayKey,
        events: evs,
      });
    }
    return groups;
  });

  readonly visibleGroups = computed<MonthGroup[]>(() => {
    if (this.showPast()) return this.monthGroups();
    return this.monthGroups().filter(g => !g.isPast);
  });

  readonly pastCount = computed(
    () => this.monthGroups().filter(g => g.isPast).length
  );

  ngOnInit(): void {
    this.loadConfigs();
  }

  private loadConfigs(): void {
    this.calendarService.getConfigs('Veranstaltungen').subscribe({
      next: configs => {
        this.configs.set(configs);
        this.loadEvents();
      },
    });
  }

  loadEvents(): void {
    this.loading.set(true);
    const now = new Date();
    const from = new Date(now.getFullYear(), now.getMonth() - 3, 1);
    const to = new Date(now.getFullYear() + 2, now.getMonth(), 1);

    this.calendarService.getEvents(from, to, 'Veranstaltungen').subscribe({
      next: events => {
        this.events.set(events);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openCreateDialog(): void {
    const writableIds = this.writableCalendarIds();
    if (writableIds.length === 0) return;

    const ref = this.dialog.open(EventFormDialogComponent, {
      width: '480px',
      maxWidth: '95vw',
      data: {
        configs: this.configs(),
        writableCalendarIds: writableIds,
      },
    });

    ref.afterClosed().subscribe((result?: EventFormDialogResult) => {
      if (!result) return;
      this.calendarService.createEvent({
        calendarId: result.calendarId,
        title: result.title,
        start: result.start.toISOString(),
        end: result.end.toISOString(),
        isAllDay: result.isAllDay,
        description: result.description,
        location: result.location,
      }).subscribe(() => this.loadEvents());
    });
  }

  openEditDialog(event: CalendarEvent): void {
    const writableIds = this.writableCalendarIds();
    if (!writableIds.includes(event.calendarId)) return;

    const ref = this.dialog.open(EventFormDialogComponent, {
      width: '480px',
      maxWidth: '95vw',
      data: {
        event,
        configs: this.configs(),
        writableCalendarIds: writableIds,
      },
    });

    ref.afterClosed().subscribe((result?: EventFormDialogResult) => {
      if (!result) return;
      this.calendarService.updateEvent(event.calendarId, event.id, {
        title: result.title,
        start: result.start.toISOString(),
        end: result.end.toISOString(),
        isAllDay: result.isAllDay,
        description: result.description,
        location: result.location,
      }).subscribe(() => this.loadEvents());
    });
  }

  deleteEvent(event: CalendarEvent, e: MouseEvent): void {
    e.stopPropagation();
    if (!this.writableCalendarIds().includes(event.calendarId)) return;
    if (!confirm(`Veranstaltung "${event.title}" wirklich löschen?`)) return;
    this.calendarService.deleteEvent(event.calendarId, event.id)
      .subscribe(() => this.loadEvents());
  }

  canEditEvent(event: CalendarEvent): boolean {
    return this.writableCalendarIds().includes(event.calendarId);
  }

  formatEventDate(event: CalendarEvent): string {
    if (!event.start) return '';
    const start = new Date(event.start);
    if (event.isAllDay) return formatDayLabel(start);
    const end = event.end ? new Date(event.end) : null;
    const time = formatTime(start);
    const endTime = end && !isSameDay(start, end) ? ` – ${formatDayLabel(end)}, ${formatTime(end)}` :
                    end ? ` – ${formatTime(end)} Uhr` : ' Uhr';
    return `${formatDayLabel(start)}, ${time}${endTime}`;
  }
}

// ---- Helpers ----------------------------------------------------------------

function monthKey(d: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}`;
}

const MONTHS = [
  'Januar', 'Februar', 'März', 'April', 'Mai', 'Juni',
  'Juli', 'August', 'September', 'Oktober', 'November', 'Dezember',
];

function formatMonthLabel(key: string): string {
  const [year, month] = key.split('-').map(Number);
  return `${MONTHS[month - 1]} ${year}`;
}

const DAYS = ['So', 'Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa'];

function formatDayLabel(d: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${DAYS[d.getDay()]}, ${pad(d.getDate())}. ${MONTHS[d.getMonth()].slice(0, 3)}.`;
}

function formatTime(d: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function isSameDay(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear()
    && a.getMonth() === b.getMonth()
    && a.getDate() === b.getDate();
}
