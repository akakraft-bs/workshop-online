using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Enums;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.WebApi.Services;

public class HallenbuchReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    ICalendarService calendarService,
    IHostEnvironment environment,
    IConfiguration configuration,
    ILogger<HallenbuchReminderBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!environment.IsProduction())
        {
            logger.LogInformation("HallenbuchReminderBackgroundService ist nur in Production aktiv – wird übersprungen.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = DateTime.Today.AddHours(22);
            if (now >= nextRun)
                nextRun = nextRun.AddDays(1);

            logger.LogInformation("Nächste Hallenbuch-Erinnerung um {Time}.", nextRun.ToString("dd.MM.yyyy HH:mm"));
            await Task.Delay(nextRun - now, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fehler im HallenbuchReminderBackgroundService.");
            }
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db          = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // 1. Load Hallenbelegung calendar IDs
        var calendarIds = await db.CalendarConfigs
            .Where(c => c.CalendarType == CalendarType.Hallenbelegung && c.IsVisible)
            .Select(c => c.GoogleCalendarId)
            .ToListAsync(ct);

        if (calendarIds.Count == 0) return;

        // 2. Fetch today's events
        var todayStart = DateTime.Today;
        var todayEnd   = DateTime.Today.AddDays(1);

        var events = (await calendarService.GetEventsAsync(calendarIds, todayStart, todayEnd)).ToList();
        if (events.Count == 0) return;

        // 3. Collect user IDs from calendar events (resolved + email fallback)
        var reservationUserIds = new HashSet<Guid>();

        var resolvedIds = events
            .Where(e => e.CreatorUserId.HasValue)
            .Select(e => e.CreatorUserId!.Value);
        foreach (var id in resolvedIds)
            reservationUserIds.Add(id);

        var unresolvedEmails = events
            .Where(e => e.CreatorUserId is null && e.CreatorEmail != null)
            .Select(e => e.CreatorEmail!)
            .Distinct()
            .ToList();

        if (unresolvedEmails.Count > 0)
        {
            var emailMatchIds = await db.Users
                .Where(u => unresolvedEmails.Contains(u.Email))
                .Select(u => u.Id)
                .ToListAsync(ct);
            foreach (var id in emailMatchIds)
                reservationUserIds.Add(id);
        }

        if (reservationUserIds.Count == 0) return;

        // 4. Find users who already wrote a Hallenbuch entry today
        var usersWithEntry = (await db.HallenbuchEintraege
            .Where(e => e.Start >= todayStart && e.Start < todayEnd
                     && reservationUserIds.Contains(e.UserId))
            .Select(e => e.UserId)
            .Distinct()
            .ToListAsync(ct))
            .ToHashSet();

        var usersNeedingReminder = reservationUserIds.Except(usersWithEntry).ToList();
        if (usersNeedingReminder.Count == 0) return;

        logger.LogInformation("{Count} Nutzer haben heute eine Reservierung aber keinen Hallenbucheintrag.", usersNeedingReminder.Count);

        // 5. Load user details + display names
        var users = await db.Users
            .Where(u => usersNeedingReminder.Contains(u.Id))
            .ToListAsync(ct);

        var prefsByUserId = await db.UserPreferences
            .Where(p => usersNeedingReminder.Contains(p.UserId) && p.DisplayName != null)
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!, ct);

        var frontendBaseUrl = configuration["Frontend:BaseUrl"] ?? string.Empty;

        // 6. Send push + email per user
        foreach (var user in users)
        {
            var displayName = prefsByUserId.GetValueOrDefault(user.Id) ?? user.Name;

            try
            {
                await pushService.SendToUsersAsync(
                    [user.Id],
                    "Hallenbucheintrag ausstehend 📋",
                    "Du hattest heute eine Reservierung – bitte trag deine Nutzung ins Hallenbuch ein.",
                    url: "/hallenbuch");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Push-Benachrichtigung für {UserId} fehlgeschlagen.", user.Id);
            }

            try
            {
                await emailService.SendHallenbuchReminderAsync(user.Email, displayName, frontendBaseUrl);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "E-Mail-Erinnerung an {Email} fehlgeschlagen.", user.Email);
            }
        }
    }
}
