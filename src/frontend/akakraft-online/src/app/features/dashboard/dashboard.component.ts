import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatBadgeModule } from '@angular/material/badge';
import { AuthService } from '../../core/auth/auth.service';
import { ApiService } from '../../core/api/api.service';
import { CalendarService } from '../../core/calendar/calendar.service';
import { UserPreferencesService } from '../../core/user/user-preferences.service';
import { BadgeService } from '../../core/badges/badge.service';
import { CalendarEvent } from '../../models/calendar.model';
import { Verbrauchsmaterial } from '../../models/verbrauchsmaterial.model';
import { Mangel } from '../../models/mangel.model';
import { Umfrage } from '../../models/umfrage.model';
import { Role } from '../../models/user.model';

export interface NavItem {
  label: string;
  description: string;
  icon: string;
  route: string;
  requiredRoles?: Role[];
  badge?: () => number;
}

const DEFAULT_FAVORITES = ['/werkzeug', '/verbrauchsmaterial'];

@Component({
  selector: 'app-dashboard',
  imports: [
    RouterLink, MatCardModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatChipsModule, MatTooltipModule, MatBadgeModule,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly api = inject(ApiService);
  private readonly calendarService = inject(CalendarService);
  private readonly prefsService = inject(UserPreferencesService);
  readonly badges = inject(BadgeService);

  readonly upcomingEvents = signal<CalendarEvent[]>([]);
  readonly loadingEvents = signal(true);
  readonly lowStockItems = signal<Verbrauchsmaterial[]>([]);
  readonly openMaengel = signal<Mangel[]>([]);
  readonly pendingUmfragen = signal<Umfrage[]>([]);

  readonly favoriteRoutes = signal<string[]>(DEFAULT_FAVORITES);
  readonly editMode = signal(false);
  readonly savingPrefs = signal(false);

  private loadedPhone: string | null = null;
  private loadedAddress: string | null = null;

  readonly allQuickItems: NavItem[] = [
    { label: 'Hallenbelegung', description: 'Belegungskalender anzeigen', icon: 'calendar_month', route: '/kalender' },
    { label: 'Veranstaltungen', description: 'Veranstaltungen planen und verwalten', icon: 'celebration', route: '/veranstaltungen' },
    { label: 'Werkzeug', description: 'Werkzeug einsehen und ausleihen', icon: 'build', route: '/werkzeug' },
    { label: 'Verbrauchsmaterial', description: 'Aktuellen Bestand einsehen', icon: 'inventory_2', route: '/verbrauchsmaterial', badge: () => this.badges.lowStock() },
    { label: 'Mängelmelder', description: 'Mängel melden und einsehen', icon: 'report_problem', route: '/mangel', badge: () => this.badges.openMaengel() },
    { label: 'Wunschliste', description: 'Neuanschaffungen vorschlagen', icon: 'playlist_add', route: '/wunsch' },
    { label: 'Umfragen', description: 'Umfragen erstellen und abstimmen', icon: 'poll', route: '/umfrage', badge: () => this.badges.pendingUmfragen() },
    { label: 'Hallenbuch', description: 'Hallenbuchzeiten eintragen und einsehen', icon: 'menu_book', route: '/hallenbuch' },
    { label: 'Nutzerverwaltung', description: 'Nutzer und Rollen verwalten', icon: 'manage_accounts', route: '/admin/users', requiredRoles: [Role.Admin] },
    { label: 'Kalender-Einstellungen', description: 'Kalender konfigurieren', icon: 'tune', route: '/admin/kalender', requiredRoles: [Role.Admin] },
    { label: 'Feedback', description: 'Eingegangenes Feedback verwalten', icon: 'feedback', route: '/admin/feedback', requiredRoles: [Role.Admin], badge: () => this.badges.unseenFeedback() },
    { label: 'Test-Benachrichtigung', description: 'Push-Benachrichtigung senden', icon: 'notifications_active', route: '/admin/push', requiredRoles: [Role.Admin] },
  ];

  readonly availableItems = computed(() =>
    this.allQuickItems.filter(item =>
      !item.requiredRoles || this.auth.hasAnyRole(item.requiredRoles)
    )
  );

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
      error: () => {},
    });

    this.api.get<Mangel[]>('/mangel').subscribe({
      next: items => this.openMaengel.set(
        items.filter(m => m.status === 'Offen' || m.status === 'Kenntnisgenommen')
      ),
      error: () => {},
    });

    this.api.get<Umfrage[]>('/umfrage').subscribe({
      next: items => this.pendingUmfragen.set(
        items.filter(u => u.status === 'Offen' && u.currentUserOptionIds.length === 0)
      ),
      error: () => {},
    });

    this.prefsService.getPreferences().subscribe({
      next: prefs => {
        if (prefs.favoriteRoutes.length > 0) {
          this.favoriteRoutes.set(prefs.favoriteRoutes);
        }
        this.loadedPhone = prefs.phone;
        this.loadedAddress = prefs.address;
      },
      error: () => {},
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
      phone: this.loadedPhone,
      address: this.loadedAddress,
    }).subscribe({
      next: prefs => { this.favoriteRoutes.set(prefs.favoriteRoutes); this.savingPrefs.set(false); },
      error: () => this.savingPrefs.set(false),
    });
  }

  formatUmfrageDeadline(deadline: string | null | undefined): string | null {
    if (!deadline) return null;
    const d = new Date(deadline);
    const diffH = Math.round((d.getTime() - Date.now()) / 3_600_000);
    if (diffH < 0) return 'Abgelaufen';
    if (diffH < 24) return `Noch ${diffH}h`;
    if (diffH < 48) return 'Morgen';
    return `${pad(d.getDate())}.${pad(d.getMonth() + 1)}.${d.getFullYear()}`;
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
