using AkaKraft.Application.Interfaces;
using AkaKraft.Infrastructure.Data;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AkaKraft.Infrastructure.Services;

public class FcmPushNotificationService(
    IServiceScopeFactory scopeFactory,
    FirebaseApp firebaseApp,
    ILogger<FcmPushNotificationService> logger) : IPushNotificationService
{
    public async Task SendToUserAsync(Guid userId, string title, string body, string? url = null)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var tokens = await db.FcmTokens
            .Where(t => t.UserId == userId)
            .Select(t => t.Token)
            .ToListAsync();

        if (tokens.Count == 0) return;

        await SendToTokensAsync(db, tokens, title, body, url);
    }

    public async Task SendToAllSubscribedAsync(string title, string body, string? url = null)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var tokens = await db.FcmTokens
            .Select(t => t.Token)
            .ToListAsync();

        if (tokens.Count == 0) return;

        await SendToTokensAsync(db, tokens, title, body, url);
    }

    public async Task SendToUsersAsync(IEnumerable<Guid> userIds, string title, string body, string? url = null)
    {
        var ids = userIds.ToList();
        if (ids.Count == 0) return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var tokens = await db.FcmTokens
            .Where(t => ids.Contains(t.UserId))
            .Select(t => t.Token)
            .ToListAsync();

        if (tokens.Count == 0) return;

        await SendToTokensAsync(db, tokens, title, body, url);
    }

    private async Task SendToTokensAsync(ApplicationDbContext db, List<string> tokens, string title, string body, string? url)
    {
        var messaging = FirebaseMessaging.GetMessaging(firebaseApp);

        // Relative URLs mit /app/-Prefix versehen
        var normalizedUrl = url is not null && url.StartsWith('/') && !url.StartsWith("/app/")
            ? "/app" + url
            : url;

        // FCM erlaubt max. 500 Tokens pro Batch
        foreach (var batch in tokens.Chunk(500))
        {
            // Data-only: kein Notification-Objekt, damit der Browser die Benachrichtigung
            // nicht automatisch zeigt UND der Service Worker sie nicht nochmal anzeigt.
            // onBackgroundMessage im SW übernimmt die Darstellung genau einmal.
            var data = new Dictionary<string, string>
            {
                ["title"] = title,
                ["body"] = body,
            };
            if (normalizedUrl is not null)
                data["url"] = normalizedUrl;

            var message = new MulticastMessage
            {
                Tokens = batch.ToList(),
                Data = data,
            };

            try
            {
                var response = await messaging.SendEachForMulticastAsync(message);
                await CleanupInvalidTokensAsync(db, batch, response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fehler beim Senden von FCM-Nachrichten.");
            }
        }
    }

    private async Task CleanupInvalidTokensAsync(
        ApplicationDbContext db,
        IReadOnlyList<string> sentTokens,
        BatchResponse response)
    {
        var invalidTokens = new List<string>();

        for (int i = 0; i < response.Responses.Count; i++)
        {
            var r = response.Responses[i];
            if (!r.IsSuccess &&
                r.Exception?.MessagingErrorCode is
                    MessagingErrorCode.Unregistered or
                    MessagingErrorCode.InvalidArgument)
            {
                invalidTokens.Add(sentTokens[i]);
            }
        }

        if (invalidTokens.Count == 0) return;

        var toRemove = await db.FcmTokens
            .Where(t => invalidTokens.Contains(t.Token))
            .ToListAsync();

        db.FcmTokens.RemoveRange(toRemove);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "{Count} abgelaufene FCM-Tokens entfernt.", toRemove.Count);
    }
}
