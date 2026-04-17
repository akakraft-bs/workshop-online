using AkaKraft.Application.Interfaces;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AkaKraft.Infrastructure.Services;

public class WerkzeugRueckgabeReminderService(
    IServiceScopeFactory scopeFactory,
    ILogger<WerkzeugRueckgabeReminderService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await WaitUntilNextRunAsync(stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                break;

            await RunCheckAsync();
        }
    }

    private static async Task WaitUntilNextRunAsync(CancellationToken stoppingToken)
    {
        var now = DateTime.Now;
        // Täglich um 09:00 Uhr ausführen
        var nextRun = DateTime.Today.AddHours(9);
        if (now >= nextRun)
            nextRun = nextRun.AddDays(1);

        var delay = nextRun - now;
        await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
    }

    private async Task RunCheckAsync()
    {
        logger.LogInformation("Werkzeug-Rückgabe-Erinnerung: Überprüfung läuft.");

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

        var tomorrow = DateTime.UtcNow.Date.AddDays(2); // Fälligkeiten bis morgen

        var overdue = await db.Werkzeuge
            .Where(w => !w.IsAvailable
                && w.BorrowedByUserId.HasValue
                && w.ExpectedReturnAt.HasValue
                && w.ExpectedReturnAt < tomorrow)
            .ToListAsync();

        foreach (var werkzeug in overdue)
        {
            var userId = werkzeug.BorrowedByUserId!.Value;
            var returnDate = werkzeug.ExpectedReturnAt!.Value.ToLocalTime();
            var isOverdue = werkzeug.ExpectedReturnAt < DateTime.UtcNow;

            string title, body;
            if (isOverdue)
            {
                title = "Werkzeug überfällig!";
                body = $"„{werkzeug.Name}" hätte am {returnDate:dd.MM.yyyy} zurückgebracht werden sollen.";
            }
            else
            {
                title = "Werkzeug-Rückgabe morgen";
                body = $"„{werkzeug.Name}" bitte bis {returnDate:dd.MM.yyyy} zurückbringen.";
            }

            try
            {
                await pushService.NotifyUsersWithPreferenceAsync(
                    p => p.WerkzeugRueckgabe,
                    title,
                    body,
                    url: "/werkzeug",
                    restrictToUserIds: [userId]);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Erinnerung für Werkzeug {WerkzeugId} konnte nicht gesendet werden.", werkzeug.Id);
            }
        }
    }
}
