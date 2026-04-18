using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;

namespace AkaKraft.WebApi.Endpoints;

public static class AdminApi
{
    public static WebApplication AddAdminApi(this WebApplication app)
    {
        // Test-Notification an einen oder alle Nutzer senden (Admin)
        app.MapPost("/admin/push/test", async (
            SendTestPushDto dto,
            IPushNotificationService pushService) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Body))
                return Results.BadRequest("Titel und Text dürfen nicht leer sein.");

            if (dto.UserId.HasValue)
                await pushService.SendToUserAsync(dto.UserId.Value, dto.Title, dto.Body);
            else
                await pushService.SendToUsersWithPreferenceAsync(_ => true, dto.Title, dto.Body);

            return Results.Ok();
        }).RequireAuthorization("AdminOnly");

        // Admin: alle Feedbacks abrufen
        app.MapGet("/admin/feedback", async (IFeedbackService feedbackService) =>
            Results.Ok(await feedbackService.GetAllAsync()))
            .RequireAuthorization("AdminOnly");

        // Admin: Status eines Feedbacks aktualisieren
        app.MapPatch("/admin/feedback/{id:guid}/status", async (
            Guid id, UpdateFeedbackStatusDto dto, IFeedbackService feedbackService) =>
        {
            var result = await feedbackService.UpdateStatusAsync(id, dto.Status);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("AdminOnly");

        // -------------------------------------------------------------------------
        // Calendar Config Endpoints (Admin)
        // -------------------------------------------------------------------------

        // Alle konfigurierten Kalender abrufen (optional nach Typ filtern)
        app.MapGet("/calendar/configs", async (ICalendarConfigService configService, string? type) =>
        {
            var all = await configService.GetAllAsync();
            if (!string.IsNullOrWhiteSpace(type))
                all = all.Where(c => string.Equals(c.CalendarType, type, StringComparison.OrdinalIgnoreCase));
            return Results.Ok(all);
        }).RequireAuthorization("AnyRole");

        // Verfügbare Google-Kalender + aktueller DB-Config (Admin)
        app.MapGet("/admin/calendar/available", async (
            ICalendarService calendarService,
            ICalendarConfigService configService) =>
        {
            var available = await calendarService.GetAvailableCalendarsAsync();
            var configs = (await configService.GetAllAsync()).ToDictionary(c => c.GoogleCalendarId);
            var availableIds = available.Select(a => a.GoogleCalendarId).ToHashSet();

            var merged = available.Select(a => new AvailableCalendarDto(
                a.GoogleCalendarId,
                a.Name,
                a.Description,
                configs.GetValueOrDefault(a.GoogleCalendarId)
            )).ToList();

            // Auch DB-konfigurierte Kalender einbeziehen, die nicht im Google-CalendarList sind
            foreach (var (id, cfg) in configs)
            {
                if (!availableIds.Contains(id))
                    merged.Add(new AvailableCalendarDto(id, cfg.Name, null, cfg));
            }

            return Results.Ok(merged);
        }).RequireAuthorization("AdminOnly");

        // Service Account bei einem Kalender anmelden (damit er in CalendarList erscheint)
        app.MapPost("/admin/calendar/subscribe", async (
            SubscribeCalendarDto dto,
            ICalendarService calendarService) =>
        {
            var result = await calendarService.SubscribeCalendarAsync(dto.CalendarId);
            if (result is null)
                return Results.Problem("Kalender konnte nicht abonniert werden. Prüfe, ob der Service Account Zugriff hat.");
            return Results.Ok(result);
        }).RequireAuthorization("AdminOnly");

        // Kalender-Konfiguration anlegen / aktualisieren (Admin)
        app.MapPut("/admin/calendar/configs/{googleCalendarId}", async (
            string googleCalendarId,
            UpdateCalendarConfigDto dto,
            ICalendarConfigService configService) =>
        {
            var result = await configService.UpsertAsync(googleCalendarId, dto);
            return Results.Ok(result);
        }).RequireAuthorization("AdminOnly");

        return app;
    }
}