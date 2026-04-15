using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AkaKraft.Infrastructure.Services;

public class GoogleCalendarService : ICalendarService
{
    private const string AppCreatorNameKey = "appCreatorName";
    private const string AppCreatorEmailKey = "appCreatorEmail";

    private readonly CalendarService? _calendarService;
    private readonly ILogger<GoogleCalendarService> _logger;

    public GoogleCalendarService(IConfiguration configuration, ILogger<GoogleCalendarService> logger)
    {
        _logger = logger;

        var serviceAccountJson = configuration["GoogleCalendar:ServiceAccountJson"];
        if (string.IsNullOrWhiteSpace(serviceAccountJson))
        {
            _logger.LogWarning("GoogleCalendar:ServiceAccountJson ist nicht konfiguriert. Kalender-Funktionen sind deaktiviert.");
            return;
        }

        try
        {
            var credential = GoogleCredential.FromJson(serviceAccountJson)
                .CreateScoped(CalendarService.Scope.Calendar);

            _calendarService = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "AkaKraft Workshop"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Initialisieren des Google Calendar Service.");
        }
    }

    public async Task<IEnumerable<AvailableCalendarDto>> GetAvailableCalendarsAsync()
    {
        if (_calendarService is null)
            return [];

        try
        {
            var request = _calendarService.CalendarList.List();
            var result = await request.ExecuteAsync();
            return result.Items?.Select(c => new AvailableCalendarDto(c.Id, c.Summary, c.Description, null)) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen der verfügbaren Kalender.");
            return [];
        }
    }

    public async Task<IEnumerable<CalendarEventDto>> GetEventsAsync(
        IEnumerable<string> calendarIds, DateTime from, DateTime to)
    {
        if (_calendarService is null)
            return [];

        var allEvents = new List<CalendarEventDto>();
        var tasks = calendarIds.Select(calId => FetchEventsForCalendarAsync(calId, from, to));
        var results = await Task.WhenAll(tasks);

        foreach (var events in results)
            allEvents.AddRange(events);

        return allEvents.OrderBy(e => e.Start ?? DateTime.MaxValue);
    }

    public async Task<CalendarEventDto> CreateEventAsync(
        string calendarId, string calendarName, string calendarColor,
        CreateCalendarEventDto dto, string creatorName, string creatorEmail)
    {
        if (_calendarService is null)
            throw new InvalidOperationException("Google Calendar Service ist nicht konfiguriert.");

        var googleEvent = BuildGoogleEvent(dto.Title, dto.Start, dto.End, dto.IsAllDay, dto.Description, dto.Location, creatorName, creatorEmail);
        var request = _calendarService.Events.Insert(googleEvent, calendarId);
        var created = await request.ExecuteAsync();

        return MapToDto(created, calendarId, calendarName, calendarColor);
    }

    public async Task<CalendarEventDto?> UpdateEventAsync(
        string calendarId, string calendarName, string calendarColor,
        string eventId, UpdateCalendarEventDto dto)
    {
        if (_calendarService is null)
            throw new InvalidOperationException("Google Calendar Service ist nicht konfiguriert.");

        var existing = await _calendarService.Events.Get(calendarId, eventId).ExecuteAsync();
        if (existing is null)
            return null;

        existing.Summary = dto.Title;
        existing.Description = dto.Description;
        existing.Location = dto.Location;
        SetEventTime(existing, dto.Start, dto.End, dto.IsAllDay);

        var updated = await _calendarService.Events.Update(existing, calendarId, eventId).ExecuteAsync();
        return MapToDto(updated, calendarId, calendarName, calendarColor);
    }

    public async Task<bool> DeleteEventAsync(string calendarId, string eventId)
    {
        if (_calendarService is null)
            throw new InvalidOperationException("Google Calendar Service ist nicht konfiguriert.");

        await _calendarService.Events.Delete(calendarId, eventId).ExecuteAsync();
        return true;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<IEnumerable<CalendarEventDto>> FetchEventsForCalendarAsync(
        string calendarId, DateTime from, DateTime to)
    {
        // We need the name & color from the config – those are injected at the call site
        // (calendarId is passed here; the caller maps calendarId → name/color separately)
        // For this internal fetch we pass placeholder values; the caller enriches them.
        try
        {
            var request = _calendarService!.Events.List(calendarId);
            request.TimeMinDateTimeOffset = new DateTimeOffset(from, TimeSpan.Zero);
            request.TimeMaxDateTimeOffset = new DateTimeOffset(to, TimeSpan.Zero);
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
            request.MaxResults = 500;

            var result = await request.ExecuteAsync();
            return result.Items?
                .Where(e => e.Status != "cancelled")
                .Select(e => MapToDto(e, calendarId, calendarId, "#1976D2"))
                ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen der Ereignisse für Kalender {CalendarId}.", calendarId);
            return [];
        }
    }

    private static Event BuildGoogleEvent(
        string title, DateTime start, DateTime end, bool isAllDay,
        string? description, string? location, string creatorName, string creatorEmail)
    {
        var ev = new Event
        {
            Summary = title,
            Description = description,
            Location = location,
            ExtendedProperties = new Event.ExtendedPropertiesData
            {
                Shared = new Dictionary<string, string>
                {
                    { AppCreatorNameKey, creatorName },
                    { AppCreatorEmailKey, creatorEmail }
                }
            }
        };

        SetEventTime(ev, start, end, isAllDay);
        return ev;
    }

    private static void SetEventTime(Event ev, DateTime start, DateTime end, bool isAllDay)
    {
        if (isAllDay)
        {
            ev.Start = new EventDateTime { Date = start.ToString("yyyy-MM-dd") };
            ev.End = new EventDateTime { Date = end.ToString("yyyy-MM-dd") };
        }
        else
        {
            ev.Start = new EventDateTime { DateTimeDateTimeOffset = new DateTimeOffset(start.ToUniversalTime()) };
            ev.End = new EventDateTime { DateTimeDateTimeOffset = new DateTimeOffset(end.ToUniversalTime()) };
        }
    }

    private static CalendarEventDto MapToDto(Event ev, string calendarId, string calendarName, string calendarColor)
    {
        bool isAllDay = ev.Start?.Date is not null;

        DateTime? start = isAllDay
            ? DateTime.Parse(ev.Start!.Date)
            : ev.Start?.DateTimeDateTimeOffset?.UtcDateTime;

        DateTime? end = isAllDay
            ? DateTime.Parse(ev.End!.Date)
            : ev.End?.DateTimeDateTimeOffset?.UtcDateTime;

        // Prefer app-set creator; fall back to Google Calendar creator field
        string? creatorName = null;
        string? creatorEmail = null;

        if (ev.ExtendedProperties?.Shared is { } shared)
        {
            shared.TryGetValue(AppCreatorNameKey, out creatorName);
            shared.TryGetValue(AppCreatorEmailKey, out creatorEmail);
        }

        creatorName ??= ev.Creator?.DisplayName;
        creatorEmail ??= ev.Creator?.Email;

        return new CalendarEventDto(
            ev.Id ?? string.Empty,
            calendarId,
            calendarName,
            calendarColor,
            ev.Summary ?? "(Kein Titel)",
            start,
            end,
            isAllDay,
            creatorName,
            creatorEmail,
            ev.Description,
            ev.Location
        );
    }
}
