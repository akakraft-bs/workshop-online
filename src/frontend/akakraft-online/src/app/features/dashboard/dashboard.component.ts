import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../core/auth/auth.service';
import { ApiService } from '../../core/api/api.service';
import { CalendarService } from '../../core/calendar/calendar.service';
import { UserPreferencesService } from '../../core/user/user-preferences.service';
import { CalendarEvent } from '../../models/calendar.model';
import { Verbrauchsmaterial } from '../../models/verbrauchsmaterial.model';
import { Mangel } from '../../models/mangel.model';
import { Role } from '../../models/user.model';

export interface NavItem {
  label: string;
  description: string;
  icon: string;
  route: string;
  requiredRoles?: Role[];
}

const ALL_QUICK_ITEMS: NavItem[] = [
  { label: 'Hallenbelegung', description: 'Belegungskalender anzeigen', icon: 'calendar_month', route: '/kalender' },
  { label: 'Veranstaltungen', description: 'Veranstaltungen planen und verwalten', icon: 'celebration', route: '/veranstaltungen' },
  { label: 'Werkzeug', description: 'Werkzeug einsehen und ausleihen', icon: 'build', route: '/werkzeug' },
  { label: 'Verbrauchsmaterial', description: 'Aktuellen Bestand einsehen', icon: 'inventory_2', route: '/verbrauchsmaterial' },
  { label: 'Mängelmelder', description: 'Mängel melden und einsehen', icon: 'report_problem', route: '/mangel' },
  { label: 'Nutzerverwaltung', description: 'Nutzer und Rollen verwalten', icon: 'manage_accounts', route: '/admin/users', requiredRoles: [Role.Admin] },
  { label: 'Kalender-Einstellungen', description: 'Kalender konfigurieren', icon: 'tune', route: '/admin/kalender', requiredRoles: [Role.Admin] },
  { label: 'Feedback', description: 'Eingegangenes Feedback verwalten', icon: 'feedback', route: '/admin/feedback', requiredRoles: [Role.Admin] },
  { label: 'Test-Benachrichtigung', description: 'Push-Benachrichtigung senden', icon: 'notifications_active', route: '/admin/push', requiredRoles: [Role.Admin] },
];

const DEFAULT_FAVORITES = ['/werkzeug', '/verbrauchsmaterial'];

@Component({
  selector: 'app-dashboard',
  imports: [
    RouterLink, MatCardModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatChipsModule, MatTooltipModule,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly api = inject(ApiService);
  private readonly calendarService = inject(CalendarService);
  private readonly prefsService = inject(UserPreferencesService);

  readonly upcomingEvents = signal<CalendarEvent[]>([]);
  readonly loadingEvents = signal(true);
  readonly lowStockItems = signal<Verbrauchsmaterial[]>([]);
  readonly openMaengel = signal<Mangel[]>([]);

  readonly favoriteRoutes = signal<string[]>(DEFAULT_FAVORITES);
  readonly editMode = signal(false);
  readonly savingPrefs = signal(false);

  private notifyLeihruckgabe = true;
  private notifyVeranstaltungen = true;
  private notifyMindestbestand = true;

  /** All nav items the current user may access (role-filtered) */
  readonly availableItems = computed(() =>
    ALL_QUICK_ITEMS.filter(item =>
      !item.requiredRoles || this.auth.hasAnyRole(item.requiredRoles)
    )
  );

  /** Items shown in the quick-access section */
  readonly quickLinks = computed(() =>
    this.availableItems().filter(item => this.favoriteRoutes().includes(item.route))
  );

  ngOnInit(): void {
    this.calendarService.getUpcomingEvents().subscribe({
      next: events => { this.upcomingEvents.set(events); this.loadingEvents.set(false); },
      error: () => this.loadingEvents.set(false),
    });

    this.api.get<Verbrauchsmaterial[]>('/verbrauchsmaterial').subscribe({
      next: items => this.lowStockItems.set(
        items.filter(v => v.minQuantity != null && v.quantity <= v.minQuantity)
      ),
      error: () => { /* kein Fehler anzeigen, Dashboard bleibt funktionsfähig */ },
    });

    this.api.get<Mangel[]>('/mangel').subscribe({
      next: items => this.openMaengel.set(
        items.filter(m => m.status === 'Offen' || m.status === 'Kenntnisgenommen')
      ),
      error: () => { /* kein Fehler anzeigen */ },
    });

    this.prefsService.getPreferences().subscribe({
      next: prefs => {
        if (prefs.favoriteRoutes.length > 0) {
          this.favoriteRoutes.set(prefs.favoriteRoutes);
        }
        this.notifyLeihruckgabe = prefs.notifyLeihruckgabe;
        this.notifyVeranstaltungen = prefs.notifyVeranstaltungen;
        this.notifyMindestbestand = prefs.notifyMindestbestand;
      },
      error: () => { /* keep defaults */ },
    });
  }

  isFavorite(route: string): boolean {
    return this.favoriteRoutes().includes(route);
  }

  toggleFavorite(route: string): void {
    const current = this.favoriteRoutes();
    const next = current.includes(route)
      ? current.filter(r => r !== route)
      : [...current, route];
    this.favoriteRoutes.set(next);
    this.savePreferences(next);
  }

  private savePreferences(routes: string[]): void {
    this.savingPrefs.set(true);
    this.prefsService.updatePreferences({
      favoriteRoutes: routes,
      displayName: this.auth.currentUser()?.displayName ?? null,
      notifyLeihruckgabe: this.notifyLeihruckgabe,
      notifyVeranstaltungen: this.notifyVeranstaltungen,
      notifyMindestbestand: this.notifyMindestbestand,
    }).subscribe({
      next: prefs => { this.favoriteRoutes.set(prefs.favoriteRoutes); this.savingPrefs.set(false); },
      error: () => this.savingPrefs.set(false),
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
