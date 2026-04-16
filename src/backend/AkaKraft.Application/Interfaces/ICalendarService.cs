using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface ICalendarService
{
    Task<IEnumerable<AvailableCalendarDto>> GetAvailableCalendarsAsync();
    Task<IEnumerable<CalendarEventDto>> GetEventsAsync(IEnumerable<string> calendarIds, DateTime from, DateTime to);
    Task<CalendarEventDto> CreateEventAsync(string calendarId, string calendarName, string calendarColor, CreateCalendarEventDto dto, string creatorName, string creatorEmail);
    Task<CalendarEventDto?> UpdateEventAsync(string calendarId, string calendarName, string calendarColor, string eventId, UpdateCalendarEventDto dto);
    Task<bool> DeleteEventAsync(string calendarId, string eventId);
    Task<AvailableCalendarDto?> SubscribeCalendarAsync(string calendarId);
}
