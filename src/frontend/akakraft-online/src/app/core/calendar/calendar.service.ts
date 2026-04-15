import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api/api.service';
import {
  AvailableCalendar,
  CalendarConfig,
  CalendarEvent,
  CreateCalendarEventRequest,
  UpdateCalendarConfigRequest,
  UpdateCalendarEventRequest,
} from '../../models/calendar.model';

@Injectable({ providedIn: 'root' })
export class CalendarService {
  private readonly api = inject(ApiService);

  getConfigs(): Observable<CalendarConfig[]> {
    return this.api.get<CalendarConfig[]>('/calendar/configs');
  }

  getEvents(from: Date, to: Date): Observable<CalendarEvent[]> {
    const fromStr = from.toISOString();
    const toStr = to.toISOString();
    return this.api.get<CalendarEvent[]>(`/calendar/events?from=${encodeURIComponent(fromStr)}&to=${encodeURIComponent(toStr)}`);
  }

  createEvent(dto: CreateCalendarEventRequest): Observable<CalendarEvent> {
    return this.api.post<CalendarEvent>('/calendar/events', dto);
  }

  updateEvent(calendarId: string, eventId: string, dto: UpdateCalendarEventRequest): Observable<CalendarEvent> {
    return this.api.put<CalendarEvent>(`/calendar/events/${encodeURIComponent(calendarId)}/${eventId}`, dto);
  }

  deleteEvent(calendarId: string, eventId: string): Observable<void> {
    return this.api.delete<void>(`/calendar/events/${encodeURIComponent(calendarId)}/${eventId}`);
  }

  // Admin
  getAvailableCalendars(): Observable<AvailableCalendar[]> {
    return this.api.get<AvailableCalendar[]>('/admin/calendar/available');
  }

  upsertConfig(googleCalendarId: string, dto: UpdateCalendarConfigRequest): Observable<CalendarConfig> {
    return this.api.put<CalendarConfig>(`/admin/calendar/configs/${encodeURIComponent(googleCalendarId)}`, dto);
  }
}
