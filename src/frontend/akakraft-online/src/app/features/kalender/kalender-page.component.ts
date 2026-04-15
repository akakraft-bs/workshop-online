import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { NgStyle } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../core/auth/auth.service';
import { CalendarService } from '../../core/calendar/calendar.service';
import { CalendarConfig, CalendarEvent, PositionedEvent } from '../../models/calendar.model';
import { Role } from '../../models/user.model';
import {
  EventFormDialogComponent,
  EventFormDialogResult,
} from './event-form-dialog/event-form-dialog.component';

const PX_PER_HOUR = 64;
const HOURS = Array.from({ length: 24 }, (_, i) => i);

interface DayColumn {
  date: Date;
  label: string;
  isToday: boolean;
  allDayEvents: CalendarEvent[];
  events: PositionedEvent[];
}

@Component({
  selector: 'app-kalender-page',
  imports: [
    NgStyle,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './kalender-page.component.html',
  styleUrl: './kalender-page.component.scss',
})
export class KalenderPageComponent implements OnInit {
  private readonly calendarService = inject(CalendarService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);

  readonly PX_PER_HOUR = PX_PER_HOUR;
  readonly HOURS = HOURS;

  readonly weekStart = signal<Date>(getWeekStart(new Date()));
  readonly loading = signal(false);
  readonly configs = signal<CalendarConfig[]>([]);
  readonly events = signal<CalendarEvent[]>([]);

  readonly days = computed<DayColumn[]>(() => {
    const start = this.weekStart();
    const evs = this.events();
    return Array.from({ length: 7 }, (_, i) => {
      const date = addDays(start, i);
      const dayEvs = evs.filter(e => isSameDay(e, date));
      return {
        date,
        label: formatDayLabel(date),
        isToday: isToday(date),
        allDayEvents: dayEvs.filter(e => e.isAllDay),
        events: dayEvs.filter(e => !e.isAllDay).map(e => toPositionedEvent(e, i)),
      };
    });
  });

  readonly weekRange = computed(() => {
    const start = this.weekStart();
    const end = addDays(start, 6);
    return `${formatDate(start)} – ${formatDate(end)}`;
  });

  readonly writableCalendarIds = computed(() => {
    const user = this.auth.currentUser();
    if (!user) return [];
    const userRoles = new Set(user.roles as string[]);

    if (userRoles.has(Role.Admin) || userRoles.has(Role.Chairman) || userRoles.has(Role.ViceChairman)) {
      return this.configs().map(c => c.googleCalendarId);
    }

    return this.configs()
      .filter(c => c.writeRoles.length === 0
        ? false
        : c.writeRoles.some(r => userRoles.has(r)))
      .map(c => c.googleCalendarId);
  });

  readonly canWrite = computed(() => this.writableCalendarIds().length > 0);

  ngOnInit(): void {
    this.loadConfigs();
  }

  private loadConfigs(): void {
    this.calendarService.getConfigs().subscribe({
      next: configs => {
        this.configs.set(configs);
        this.loadEvents();
      },
    });
  }

  loadEvents(): void {
    this.loading.set(true);
    const start = this.weekStart();
    const end = addDays(start, 7);
    this.calendarService.getEvents(start, end).subscribe({
      next: events => {
        this.events.set(events);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  prevWeek(): void {
    this.weekStart.set(addDays(this.weekStart(), -7));
    this.loadEvents();
  }

  nextWeek(): void {
    this.weekStart.set(addDays(this.weekStart(), 7));
    this.loadEvents();
  }

  goToToday(): void {
    this.weekStart.set(getWeekStart(new Date()));
    this.loadEvents();
  }

  openCreateDialog(defaultStart?: Date): void {
    const configs = this.configs().filter(c => c.isVisible);
    const writableIds = this.writableCalendarIds();
    if (writableIds.length === 0) return;

    const ref = this.dialog.open(EventFormDialogComponent, {
      width: '480px',
      maxWidth: '95vw',
      data: { configs, writableCalendarIds: writableIds, defaultStart },
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
    const configs = this.configs().filter(c => c.isVisible);
    const writableIds = this.writableCalendarIds();
    if (!writableIds.includes(event.calendarId)) return;

    const ref = this.dialog.open(EventFormDialogComponent, {
      width: '480px',
      maxWidth: '95vw',
      data: { event, configs, writableCalendarIds: writableIds },
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
    if (!confirm(`Termin "${event.title}" wirklich löschen?`)) return;

    this.calendarService.deleteEvent(event.calendarId, event.id)
      .subscribe(() => this.loadEvents());
  }

  canEditEvent(event: CalendarEvent): boolean {
    return this.writableCalendarIds().includes(event.calendarId);
  }

  getConfigColor(calendarId: string): string {
    return this.configs().find(c => c.googleCalendarId === calendarId)?.color ?? '#1976D2';
  }

  eventStyle(ev: PositionedEvent): Record<string, string> {
    return {
      top: `${ev.topPx}px`,
      height: `${Math.max(ev.heightPx, 24)}px`,
      'background-color': ev.event.calendarColor,
    };
  }

  trackByEventId(_: number, ev: PositionedEvent): string {
    return ev.event.id;
  }

  trackByAllDay(_: number, ev: CalendarEvent): string {
    return ev.id;
  }

  trackByHour(_: number, h: number): number {
    return h;
  }

  formatHour(h: number): string {
    return h.toString().padStart(2, '0') + ':00';
  }

  onGridClick(day: DayColumn, e: MouseEvent): void {
    if (!this.canWrite()) return;
    const target = e.currentTarget as HTMLElement;
    const rect = target.getBoundingClientRect();
    const offsetY = e.clientY - rect.top;
    const hour = Math.floor(offsetY / PX_PER_HOUR);
    const defaultStart = new Date(day.date);
    defaultStart.setHours(Math.min(hour, 23), 0, 0, 0);
    this.openCreateDialog(defaultStart);
  }
}

// -------------------------------------------------------------------------
// Helpers
// -------------------------------------------------------------------------

function getWeekStart(d: Date): Date {
  const day = new Date(d);
  day.setHours(0, 0, 0, 0);
  const dow = day.getDay(); // 0=Sun
  const diff = dow === 0 ? -6 : 1 - dow; // adjust to Monday
  day.setDate(day.getDate() + diff);
  return day;
}

function addDays(d: Date, days: number): Date {
  const result = new Date(d);
  result.setDate(result.getDate() + days);
  return result;
}

function isToday(d: Date): boolean {
  const today = new Date();
  return d.getFullYear() === today.getFullYear()
    && d.getMonth() === today.getMonth()
    && d.getDate() === today.getDate();
}

function isSameDay(event: CalendarEvent, date: Date): boolean {
  if (!event.start) return false;
  const evDate = new Date(event.start);
  return evDate.getFullYear() === date.getFullYear()
    && evDate.getMonth() === date.getMonth()
    && evDate.getDate() === date.getDate();
}

function toPositionedEvent(event: CalendarEvent, dayIndex: number): PositionedEvent {
  const start = new Date(event.start!);
  const end = new Date(event.end ?? event.start!);
  const startMinutes = start.getHours() * 60 + start.getMinutes();
  const durationMinutes = Math.max((end.getTime() - start.getTime()) / 60000, 30);
  return {
    event,
    dayIndex,
    topPx: (startMinutes / 60) * PX_PER_HOUR,
    heightPx: (durationMinutes / 60) * PX_PER_HOUR,
  };
}

function formatDayLabel(d: Date): string {
  const days = ['So', 'Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa'];
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${days[d.getDay()]} ${pad(d.getDate())}.${pad(d.getMonth() + 1)}.`;
}

function formatDate(d: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${pad(d.getDate())}.${pad(d.getMonth() + 1)}.${d.getFullYear()}`;
}
