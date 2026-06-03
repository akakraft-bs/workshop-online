using AkaKraft.Application.Interfaces;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.WebApi.Services;

public class CalendarUserResolutionBackgroundService(
    IServiceScopeFactory scopeFactory,
    ICalendarService calendarService,
    IHostEnvironment environment,
    ILogger<CalendarUserResolutionBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(3);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!environment.IsProduction())
        {
            logger.LogInformation("CalendarUserResolutionBackgroundService ist nur in Production aktiv – wird übersprungen.");
            return;
        }

        await Task.Delay(StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fehler im CalendarUserResolutionBackgroundService.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var calendarIds = await db.CalendarConfigs
            .Select(c => c.GoogleCalendarId)
            .ToListAsync(ct);

        if (calendarIds.Count == 0) return;

        var from = DateTime.UtcNow.Date;
        var to = from.AddYears(1);

        var allEvents = (await calendarService.GetEventsAsync(calendarIds, from, to)).ToList();

        var unresolved = allEvents
            .Where(e => e.CreatorUserId is null && e.CreatorEmail is not null)
            .ToList();

        if (unresolved.Count == 0) return;

        var emails = unresolved.Select(e => e.CreatorEmail!).Distinct().ToList();

        var users = await db.Users
            .Where(u => emails.Contains(u.Email))
            .Select(u => new { u.Id, u.Email, u.Name })
            .ToListAsync(ct);

        var userIds = users.Select(u => u.Id).ToList();

        var prefsByUserId = await db.UserPreferences
            .Where(p => userIds.Contains(p.UserId) && p.DisplayName != null)
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!, ct);

        var usersByEmail = users.ToDictionary(
            u => u.Email,
            u => (u.Id, DisplayName: prefsByUserId.GetValueOrDefault(u.Id) ?? u.Name),
            StringComparer.OrdinalIgnoreCase);

        int resolved = 0;
        foreach (var ev in unresolved)
        {
            if (!usersByEmail.TryGetValue(ev.CreatorEmail!, out var user)) continue;

            try
            {
                await calendarService.PatchEventCreatorAsync(
                    ev.CalendarId, ev.Id, user.Id, user.DisplayName, ev.CreatorEmail!);
                resolved++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Fehler beim Patchen von Kalender-Eintrag {EventId}.", ev.Id);
            }
        }

        if (resolved > 0)
            logger.LogInformation("{Count} Kalender-Eintraege mit Nutzer-ID und Name versehen.", resolved);
    }
}
