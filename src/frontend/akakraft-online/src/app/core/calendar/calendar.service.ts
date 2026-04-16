import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api/api.service';
import {
  AvailableCalendar,
  CalendarConfig,
  CalendarEvent,
  CalendarType,
  CreateCalendarEventRequest,
  UpdateCalendarConfigRequest,
  UpdateCalendarEventRequest,
} from '../../models/calendar.model';

@Injectable({ providedIn: 'root' })
export class CalendarService {
  private readonly api = inject(ApiService);

  getConfigs(type?: CalendarType): Observable<CalendarConfig[]> {
    const url = type ? `/calendar/configs?type=${encodeURIComponent(type)}` : '/calendar/configs';
    return this.api.get<CalendarConfig[]>(url);
  }

  getEvents(from: Date, to: Date, type?: CalendarType): Observable<CalendarEvent[]> {
    const fromStr = from.toISOString();
    const toStr = to.toISOString();
    let url = `/calendar/events?from=${encodeURIComponent(fromStr)}&to=${encodeURIComponent(toStr)}`;
    if (type) url += `&type=${encodeURIComponent(type)}`;
    return this.api.get<CalendarEvent[]>(url);
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

  subscribeCalendar(calendarId: string): Observable<AvailableCalendar> {
    return this.api.post<AvailableCalendar>('/admin/calendar/subscribe', { calendarId });
  }

  getUpcomingEvents(): Observable<CalendarEvent[]> {
    return this.api.get<CalendarEvent[]>('/calendar/upcoming-events');
  }
}
