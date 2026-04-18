using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Enums;

namespace AkaKraft.WebApi.Endpoints;

public static class CalendarApi
{
    public static WebApplication AddCalendarApi(this WebApplication app)
    {
        // -------------------------------------------------------------------------
        // Calendar Event Endpoints
        // -------------------------------------------------------------------------

        // Nächste Veranstaltungen für das Dashboard
        app.MapGet("/calendar/upcoming-events", async (
            ICalendarService calendarService,
            ICalendarConfigService configService) =>
        {
            var now = DateTime.UtcNow;
            var configs = (await configService.GetAllAsync())
                .Where(c => string.Equals(c.CalendarType, "Veranstaltungen", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (configs.Count == 0)
                return Results.Ok(Array.Empty<object>());

            var configMap = configs.ToDictionary(c => c.GoogleCalendarId);
            var calendarIds = configs.Select(c => c.GoogleCalendarId);

            // Erst die nächsten 14 Tage laden
            var events = (await calendarService.GetEventsAsync(calendarIds, now, now.AddDays(14)))
                .OrderBy(e => e.Start ?? DateTime.MaxValue)
                .ToList();

            // Weniger als 2 Treffer → weiter in die Zukunft schauen und mindestens 2 nehmen
            if (events.Count < 2)
            {
                events = (await calendarService.GetEventsAsync(calendarIds, now, now.AddDays(365)))
                    .OrderBy(e => e.Start ?? DateTime.MaxValue)
                    .Take(2)
                    .ToList();
            }

            var enriched = events.Select(e =>
                configMap.TryGetValue(e.CalendarId, out var cfg)
                    ? e with { CalendarName = cfg.Name, CalendarColor = cfg.Color }
                    : e);

            return Results.Ok(enriched);
        }).RequireAuthorization("AnyRole");

        // Ereignisse für einen Zeitraum abrufen
        app.MapGet("/calendar/events", async (
            DateTime from,
            DateTime to,
            string? type,
            ICalendarService calendarService,
            ICalendarConfigService configService) =>
        {
            var all = await configService.GetAllAsync();
            // Wenn ein Typ angegeben: nach Typ filtern (unabhängig von IsVisible)
            // Ohne Typ: nur sichtbare Kalender (bestehende Hallenbelegung-Logik)
            var configs = (!string.IsNullOrWhiteSpace(type)
                ? all.Where(c => string.Equals(c.CalendarType, type, StringComparison.OrdinalIgnoreCase))
                : all.Where(c => c.IsVisible))
                .ToList();

            if (configs.Count == 0)
                return Results.Ok(Array.Empty<object>());

            var events = await calendarService.GetEventsAsync(
                configs.Select(c => c.GoogleCalendarId), from, to);

            // Ereignisse mit Kalender-Name und -Farbe anreichern
            var configMap = configs.ToDictionary(c => c.GoogleCalendarId);
            var enriched = events
                .Where(e => configMap.ContainsKey(e.CalendarId))
                .Select(e =>
                {
                    var cfg = configMap[e.CalendarId];
                    return e with { CalendarName = cfg.Name, CalendarColor = cfg.Color };
                })
                .ToList();

            return Results.Ok(enriched);
        }).RequireAuthorization("AnyRole");

        // Ereignis erstellen
        app.MapPost("/calendar/events", async (
            CreateCalendarEventDto dto,
            HttpContext ctx,
            ICalendarService calendarService,
            ICalendarConfigService configService,
            IUserService userService,
            IUserPreferencesService prefsService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var config = (await configService.GetAllAsync())
                .FirstOrDefault(c => c.GoogleCalendarId == dto.CalendarId);

            if (config is null)
                return Results.NotFound("Kalender nicht gefunden.");

            if (!HasWriteAccess(ctx, config))
                return Results.Forbid();

            var user = await userService.GetByIdAsync(parsedUserId);
            if (user is null)
                return Results.Unauthorized();

            // DisplayName aus den Nutzerpräferenzen verwenden, Fallback auf Google-Name
            var prefs = await prefsService.GetAsync(parsedUserId);
            var creatorName = !string.IsNullOrWhiteSpace(prefs.DisplayName)
                ? prefs.DisplayName
                : user.Name;

            var created = await calendarService.CreateEventAsync(
                dto.CalendarId, config.Name, config.Color, dto, creatorName, user.Email);

            return Results.Created($"/calendar/events/{dto.CalendarId}/{created.Id}", created);
        }).RequireAuthorization("AnyRole");

        // Ereignis aktualisieren
        app.MapPut("/calendar/events/{calendarId}/{eventId}", async (
            string calendarId,
            string eventId,
            UpdateCalendarEventDto dto,
            HttpContext ctx,
            ICalendarService calendarService,
            ICalendarConfigService configService) =>
        {
            var config = (await configService.GetAllAsync())
                .FirstOrDefault(c => c.GoogleCalendarId == calendarId);

            if (config is null)
                return Results.NotFound("Kalender nicht gefunden.");

            if (!HasWriteAccess(ctx, config))
                return Results.Forbid();

            var updated = await calendarService.UpdateEventAsync(
                calendarId, config.Name, config.Color, eventId, dto);

            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).RequireAuthorization("AnyRole");

        // Ereignis löschen
        app.MapDelete("/calendar/events/{calendarId}/{eventId}", async (
            string calendarId,
            string eventId,
            HttpContext ctx,
            ICalendarService calendarService,
            ICalendarConfigService configService) =>
        {
            var config = (await configService.GetAllAsync())
                .FirstOrDefault(c => c.GoogleCalendarId == calendarId);

            if (config is null)
                return Results.NotFound("Kalender nicht gefunden.");

            if (!HasWriteAccess(ctx, config))
                return Results.Forbid();

            await calendarService.DeleteEventAsync(calendarId, eventId);
            return Results.NoContent();
        }).RequireAuthorization("AnyRole");
        return app;
    }

    private static bool HasWriteAccess(HttpContext ctx, AkaKraft.Application.DTOs.CalendarConfigDto config)
    {
        var userRoles = ctx.User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToHashSet();

        // Admins und Chairman dürfen immer schreiben
        if (userRoles.Contains(Role.Admin.ToString()) ||
            userRoles.Contains(Role.Chairman.ToString()) ||
            userRoles.Contains(Role.ViceChairman.ToString()))
            return true;

        // Wenn keine spezifischen Rollen konfiguriert: nur Vorstand+Admin (bereits oben)
        var writeRoles = config.WriteRoles.ToList();
        if (writeRoles.Count == 0)
            return false;

        return writeRoles.Any(r => userRoles.Contains(r));
    }

}