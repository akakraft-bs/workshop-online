import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../core/auth/auth.service';
import { CalendarService } from '../../core/calendar/calendar.service';
import { CalendarEvent } from '../../models/calendar.model';

interface QuickLink {
  label: string;
  description: string;
  icon: string;
  route: string;
}

const QUICK_LINKS: QuickLink[] = [
  { label: 'Werkzeug', description: 'Werkzeug einsehen und ausleihen', icon: 'build', route: '/werkzeug' },
  { label: 'Verbrauchsmaterial', description: 'Aktuellen Bestand einsehen', icon: 'inventory_2', route: '/verbrauchsmaterial' },
];

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, MatCardModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly calendarService = inject(CalendarService);

  readonly quickLinks = QUICK_LINKS;
  readonly upcomingEvents = signal<CalendarEvent[]>([]);
  readonly loadingEvents = signal(true);

  ngOnInit(): void {
    this.calendarService.getUpcomingEvents().subscribe({
      next: events => {
        this.upcomingEvents.set(events);
        this.loadingEvents.set(false);
      },
      error: () => this.loadingEvents.set(false),
    });
  }

  formatEventDate(event: CalendarEvent): string {
    if (!event.start) return '';
    const start = new Date(event.start);

    if (event.isAllDay) {
      return formatDate(start);
    }

    const end = event.end ? new Date(event.end) : null;
    const datePart = formatDate(start);
    const startTime = formatTime(start);
    const endTime = end ? `–${formatTime(end)}` : '';
    return `${datePart}, ${startTime}${endTime} Uhr`;
  }
}

const DAYS = ['So', 'Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa'];
const pad = (n: number) => n.toString().padStart(2, '0');

function formatDate(d: Date): string {
  return `${DAYS[d.getDay()]}, ${pad(d.getDate())}.${pad(d.getMonth() + 1)}.${d.getFullYear()}`;
}

function formatTime(d: Date): string {
  return `${pad(d.getHours())}:${pad(d.getMinutes())}`;
}
