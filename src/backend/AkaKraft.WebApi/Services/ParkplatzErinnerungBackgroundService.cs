using AkaKraft.Application.Interfaces;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.WebApi.Services;

/// <summary>
/// Läuft alle 10 Minuten und erinnert Nutzer mit aktivem Parkkonto:
/// 1. 2 Stunden vor der 24-Stunden-Grenze.
/// 2. Wenn die 24-Stunden-Grenze erreicht ist (Konto ist jetzt automatisch frei).
/// </summary>
public class ParkplatzErinnerungBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<ParkplatzErinnerungBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(40), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fehler im ParkplatzErinnerungBackgroundService.");
            }

            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var push = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

        var now = DateTime.UtcNow;

        var claims = await db.ParkClaims
            .Include(c => c.ParkAccount)
            .Where(c => c.FreigegebenAt == null
                     && (!c.Erinnerung2hGesendet || !c.ErinnerungAblaufGesendet))
            .ToListAsync(ct);

        foreach (var claim in claims)
        {
            var label = claim.ParkAccount.Label;

            if (!claim.Erinnerung2hGesendet && claim.AutoExpiresAt > now && claim.AutoExpiresAt <= now.AddHours(2))
            {
                await SafeSendAsync(push, claim.UserId,
                    $"{label}: 24-Stunden-Grenze rückt näher",
                    $"Bitte fahre {claim.Kennzeichen} vom Campus und gib {label} frei.");
                claim.Erinnerung2hGesendet = true;
            }

            if (!claim.ErinnerungAblaufGesendet && claim.AutoExpiresAt <= now)
            {
                await SafeSendAsync(push, claim.UserId,
                    $"{label}: Parkberechtigung abgelaufen",
                    $"Die 24 Stunden für {label} sind vorbei. Das Konto gilt jetzt wieder als frei – dein Fahrzeug muss den Campus verlassen haben.");
                claim.Erinnerung2hGesendet = true;
                claim.ErinnerungAblaufGesendet = true;
            }
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(ct);
    }

    private async Task SafeSendAsync(IPushNotificationService push, Guid userId, string title, string body)
    {
        try
        {
            await push.SendToUserAsync(userId, title, body, url: "/parkplatz");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Parkplatz-Push an {UserId} fehlgeschlagen.", userId);
        }
    }
}
