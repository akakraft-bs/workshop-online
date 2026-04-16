import { Role } from './user.model';

export type CalendarType = 'Hallenbelegung' | 'Veranstaltungen' | 'Hallendienst';

export const CALENDAR_TYPES: { value: CalendarType; label: string }[] = [
  { value: 'Hallenbelegung', label: 'Hallenbelegung' },
  { value: 'Veranstaltungen', label: 'Veranstaltungen' },
  { value: 'Hallendienst', label: 'Hallendienst' },
];

export interface CalendarConfig {
  id: string;
  googleCalendarId: string;
  name: string;
  color: string;
  isVisible: boolean;
  sortOrder: number;
  calendarType: CalendarType;
  writeRoles: string[];
}

export interface AvailableCalendar {
  googleCalendarId: string;
  name: string;
  description?: string;
  config?: CalendarConfig;
}

export interface CalendarEvent {
  id: string;
  calendarId: string;
  calendarName: string;
  calendarColor: string;
  title: string;
  start?: string; // ISO datetime string
  end?: string;
  isAllDay: boolean;
  creatorName?: string;
  creatorEmail?: string;
  description?: string;
  location?: string;
}

export interface CreateCalendarEventRequest {
  calendarId: string;
  title: string;
  start: string;
  end: string;
  isAllDay: boolean;
  description?: string;
  location?: string;
}

export interface UpdateCalendarEventRequest {
  title: string;
  start: string;
  end: string;
  isAllDay: boolean;
  description?: string;
  location?: string;
}

export interface UpdateCalendarConfigRequest {
  name: string;
  color: string;
  isVisible: boolean;
  sortOrder: number;
  calendarType: CalendarType;
  writeRoles: string[];
}

/** Intern berechnetes Event-Objekt für die Darstellung in der Wochenansicht */
export interface PositionedEvent {
  event: CalendarEvent;
  dayIndex: number;     // 0-6 für den Tag in der aktuellen Woche
  topPx: number;        // Pixelabstand von oben (für normale Events)
  heightPx: number;     // Höhe in Pixel
}
