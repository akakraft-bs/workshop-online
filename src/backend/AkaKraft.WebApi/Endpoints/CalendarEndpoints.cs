using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Enums;

namespace AkaKraft.WebApi.Endpoints;

internal static class CalendarEndpoints
{
    internal static void MapCalendarEndpoints(this WebApplication app)
    {
        // ---- Kalender-Konfiguration ----

        app.MapGet("/calendar/configs", async (ICalendarConfigService configService, string? type) =>
        {
            var all = await configService.GetAllAsync();
            if (!string.IsNullOrWhiteSpace(type))
                all = all.Where(c => string.Equals(c.CalendarType, type, StringComparison.OrdinalIgnoreCase));
            return Results.Ok(all);
        }).RequireAuthorization("AnyRole");

        app.MapGet("/admin/calendar/available", async (
            ICalendarService calendarService, ICalendarConfigService configService) =>
        {
            var available = await calendarService.GetAvailableCalendarsAsync();
            var configs = (await configService.GetAllAsync()).ToDictionary(c => c.GoogleCalendarId);
            var availableIds = available.Select(a => a.GoogleCalendarId).ToHashSet();

            var merged = available.Select(a => new AvailableCalendarDto(
                a.GoogleCalendarId, a.Name, a.Description,
                configs.GetValueOrDefault(a.GoogleCalendarId)
            )).ToList();

            foreach (var (id, cfg) in configs)
                if (!availableIds.Contains(id))
                    merged.Add(new AvailableCalendarDto(id, cfg.Name, null, cfg));

            return Results.Ok(merged);
        }).RequireAuthorization("AdminOnly");

        app.MapPost("/admin/calendar/subscribe", async (
            SubscribeCalendarDto dto, ICalendarService calendarService) =>
        {
            var result = await calendarService.SubscribeCalendarAsync(dto.CalendarId);
            return result is null
                ? Results.Problem("Kalender konnte nicht abonniert werden.")
                : Results.Ok(result);
        }).RequireAuthorization("AdminOnly");

        app.MapPut("/admin/calendar/configs/{googleCalendarId}", async (
            string googleCalendarId, UpdateCalendarConfigDto dto, ICalendarConfigService configService) =>
        {
            var result = await configService.UpsertAsync(googleCalendarId, dto);
            return Results.Ok(result);
        }).RequireAuthorization("AdminOnly");

        // ---- Kalender-Ereignisse ----

        app.MapGet("/calendar/upcoming-events", async (
            ICalendarService calendarService, ICalendarConfigService configService) =>
        {
            var now = DateTime.UtcNow;
            var configs = (await configService.GetAllAsync())
                .Where(c => string.Equals(c.CalendarType, "Veranstaltungen", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (configs.Count == 0) return Results.Ok(Array.Empty<object>());

            var configMap = configs.ToDictionary(c => c.GoogleCalendarId);
            var calendarIds = configs.Select(c => c.GoogleCalendarId);

            var events = (await calendarService.GetEventsAsync(calendarIds, now, now.AddDays(14)))
                .OrderBy(e => e.Start ?? DateTime.MaxValue).ToList();

            if (events.Count < 2)
                events = (await calendarService.GetEventsAsync(calendarIds, now, now.AddDays(365)))
                    .OrderBy(e => e.Start ?? DateTime.MaxValue).Take(2).ToList();

            return Results.Ok(events.Select(e =>
                configMap.TryGetValue(e.CalendarId, out var cfg)
                    ? e with { CalendarName = cfg.Name, CalendarColor = cfg.Color } : e));
        }).RequireAuthorization("AnyRole");

        app.MapGet("/calendar/events", async (
            DateTime from, DateTime to, string? type,
            ICalendarService calendarService, ICalendarConfigService configService) =>
        {
            var all = await configService.GetAllAsync();
            var configs = (!string.IsNullOrWhiteSpace(type)
                ? all.Where(c => string.Equals(c.CalendarType, type, StringComparison.OrdinalIgnoreCase))
                : all.Where(c => c.IsVisible)).ToList();

            if (configs.Count == 0) return Results.Ok(Array.Empty<object>());

            var events = await calendarService.GetEventsAsync(configs.Select(c => c.GoogleCalendarId), from, to);
            var configMap = configs.ToDictionary(c => c.GoogleCalendarId);

            return Results.Ok(events
                .Where(e => configMap.ContainsKey(e.CalendarId))
                .Select(e => { var cfg = configMap[e.CalendarId]; return e with { CalendarName = cfg.Name, CalendarColor = cfg.Color }; })
                .ToList());
        }).RequireAuthorization("AnyRole");

        app.MapPost("/calendar/events", async (
            CreateCalendarEventDto dto, HttpContext ctx,
            ICalendarService calendarService, ICalendarConfigService configService,
            IUserService userService, IUserPreferencesService prefsService) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId))
                return Results.Unauthorized();

            var config = (await configService.GetAllAsync())
                .FirstOrDefault(c => c.GoogleCalendarId == dto.CalendarId);

            if (config is null) return Results.NotFound("Kalender nicht gefunden.");
            if (!HasWriteAccess(ctx, config)) return Results.Forbid();

            var user = await userService.GetByIdAsync(userId);
            if (user is null) return Results.Unauthorized();

            var prefs = await prefsService.GetAsync(userId);
            var creatorName = !string.IsNullOrWhiteSpace(prefs.DisplayName) ? prefs.DisplayName : user.Name;

            var created = await calendarService.CreateEventAsync(
                dto.CalendarId, config.Name, config.Color, dto, creatorName, user.Email);

            return Results.Created($"/calendar/events/{dto.CalendarId}/{created.Id}", created);
        }).RequireAuthorization("AnyRole");

        app.MapPut("/calendar/events/{calendarId}/{eventId}", async (
            string calendarId, string eventId, UpdateCalendarEventDto dto,
            HttpContext ctx, ICalendarService calendarService, ICalendarConfigService configService) =>
        {
            var config = (await configService.GetAllAsync())
                .FirstOrDefault(c => c.GoogleCalendarId == calendarId);

            if (config is null) return Results.NotFound("Kalender nicht gefunden.");
            if (!HasWriteAccess(ctx, config)) return Results.Forbid();

            var updated = await calendarService.UpdateEventAsync(calendarId, config.Name, config.Color, eventId, dto);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).RequireAuthorization("AnyRole");

        app.MapDelete("/calendar/events/{calendarId}/{eventId}", async (
            string calendarId, string eventId,
            HttpContext ctx, ICalendarService calendarService, ICalendarConfigService configService) =>
        {
            var config = (await configService.GetAllAsync())
                .FirstOrDefault(c => c.GoogleCalendarId == calendarId);

            if (config is null) return Results.NotFound("Kalender nicht gefunden.");
            if (!HasWriteAccess(ctx, config)) return Results.Forbid();

            await calendarService.DeleteEventAsync(calendarId, eventId);
            return Results.NoContent();
        }).RequireAuthorization("AnyRole");
    }

    private static bool HasWriteAccess(HttpContext ctx, CalendarConfigDto config)
    {
        var userRoles = ctx.User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToHashSet();

        if (userRoles.Contains(Role.Admin.ToString()) ||
            userRoles.Contains(Role.Chairman.ToString()) ||
            userRoles.Contains(Role.ViceChairman.ToString()))
            return true;

        var writeRoles = config.WriteRoles.ToList();
        return writeRoles.Count > 0 && writeRoles.Any(r => userRoles.Contains(r));
    }
}
