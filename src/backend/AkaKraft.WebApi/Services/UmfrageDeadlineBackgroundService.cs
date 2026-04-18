using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Enums;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.WebApi.Services;

/// <summary>
/// Background service that runs every 5 minutes:
/// 1. Auto-closes polls whose deadline has passed.
/// 2. Sends a 1-hour-before reminder notification for polls with approaching deadlines.
/// </summary>
public class UmfrageDeadlineBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<UmfrageDeadlineBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Small startup delay so the app is fully initialized
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fehler im UmfrageDeadlineBackgroundService.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var push = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

        var now = DateTime.UtcNow;

        // 1. Auto-close polls whose deadline has passed
        var expired = await db.Umfragen
            .Where(u => u.Status == UmfrageStatus.Offen
                     && u.Deadline != null
                     && u.Deadline <= now)
            .ToListAsync(ct);

        foreach (var u in expired)
        {
            u.Status = UmfrageStatus.Geschlossen;
            u.ClosedAt = now;
            logger.LogInformation("Umfrage {Id} automatisch geschlossen (Fristablauf).", u.Id);
        }

        if (expired.Count > 0)
            await db.SaveChangesAsync(ct);

        // 2. Send 1-hour reminder for polls expiring within the next 50–70 minutes
        var reminderWindow = now.AddMinutes(70);
        var reminderFloor = now.AddMinutes(50);

        var upcoming = await db.Umfragen
            .Where(u => u.Status == UmfrageStatus.Offen
                     && u.Deadline != null
                     && u.Deadline > reminderFloor
                     && u.Deadline <= reminderWindow
                     && u.DeadlineReminderSentAt == null)
            .ToListAsync(ct);

        foreach (var u in upcoming)
        {
            var question = u.Question.Length > 60 ? u.Question[..57] + "…" : u.Question;
            await push.SendToUsersWithPreferenceAsync(
                p => p.NotifyUmfragen,
                "Umfrage läuft bald ab ⏰",
                $"Noch ~1 Stunde: {question}",
                url: "/umfrage");

            u.DeadlineReminderSentAt = now;
            logger.LogInformation("Erinnerungsbenachrichtigung für Umfrage {Id} gesendet.", u.Id);
        }

        if (upcoming.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
