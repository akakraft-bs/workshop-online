import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CalendarService } from '../../../core/calendar/calendar.service';
import { AvailableCalendar, CALENDAR_TYPES } from '../../../models/calendar.model';
import { ROLE_LABELS, Role } from '../../../models/user.model';

const ASSIGNABLE_ROLES: Role[] = [
  Role.Member,
  Role.Getraenkewart,
  Role.Grillwart,
  Role.Hallenwart,
  Role.Veranstaltungswart,
  Role.Treasurer,
  Role.ViceChairman,
  Role.Chairman,
];

@Component({
  selector: 'app-admin-kalender',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatChipsModule,
    MatDividerModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSnackBarModule,
  ],
  templateUrl: './admin-kalender.component.html',
  styleUrl: './admin-kalender.component.scss',
})
export class AdminKalenderComponent implements OnInit {
  private readonly calendarService = inject(CalendarService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(false);
  readonly saving = signal<string | null>(null);
  readonly subscribing = signal(false);
  readonly calendars = signal<AvailableCalendar[]>([]);
  readonly assignableRoles = ASSIGNABLE_ROLES;
  readonly roleLabels = ROLE_LABELS;
  readonly calendarTypes = CALENDAR_TYPES;

  readonly newCalendarIdControl = this.fb.control('');

  // Per-calendar form groups (keyed by googleCalendarId)
  readonly forms: Record<string, ReturnType<typeof this.buildForm>> = {};

  ngOnInit(): void {
    this.loadCalendars();
  }

  loadCalendars(): void {
    this.loading.set(true);
    this.calendarService.getAvailableCalendars().subscribe({
      next: cals => {
        this.calendars.set(cals);
        for (const cal of cals) {
          this.forms[cal.googleCalendarId] = this.buildForm(cal);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  save(googleCalendarId: string): void {
    const form = this.forms[googleCalendarId];
    if (!form || form.invalid) return;

    const v = form.getRawValue();
    this.saving.set(googleCalendarId);

    this.calendarService.upsertConfig(googleCalendarId, {
      name: v.name ?? '',
      color: v.color ?? '',
      isVisible: !!v.isVisible,
      sortOrder: v.sortOrder ?? 0,
      calendarType: v.calendarType ?? 'Hallenbelegung',
      writeRoles: (v.writeRoles as string[]) ?? [],
    }).subscribe({
      next: () => {
        this.saving.set(null);
        this.snackBar.open('Kalender-Einstellungen gespeichert.', 'OK', { duration: 3000 });
        this.loadCalendars();
      },
      error: () => {
        this.saving.set(null);
        this.snackBar.open('Fehler beim Speichern.', 'OK', { duration: 4000 });
      },
    });
  }

  subscribe(): void {
    const calendarId = this.newCalendarIdControl.value?.trim();
    if (!calendarId) return;

    this.subscribing.set(true);
    this.calendarService.subscribeCalendar(calendarId).subscribe({
      next: () => {
        this.subscribing.set(false);
        this.newCalendarIdControl.setValue('');
        this.snackBar.open('Kalender erfolgreich abonniert.', 'OK', { duration: 3000 });
        this.loadCalendars();
      },
      error: () => {
        this.subscribing.set(false);
        this.snackBar.open('Fehler beim Abonnieren. Prüfe die Kalender-ID und den Service Account Zugriff.', 'OK', { duration: 5000 });
      },
    });
  }

  getRoleLabel(role: Role): string {
    return ROLE_LABELS[role];
  }

  private buildForm(cal: AvailableCalendar) {
    const cfg = cal.config;
    return this.fb.group({
      name: [cfg?.name ?? cal.name],
      color: [cfg?.color ?? '#1976D2'],
      isVisible: [cfg?.isVisible ?? true],
      sortOrder: [cfg?.sortOrder ?? 0],
      calendarType: [cfg?.calendarType ?? 'Hallenbelegung'],
      writeRoles: [cfg?.writeRoles ?? []],
    });
  }
}
