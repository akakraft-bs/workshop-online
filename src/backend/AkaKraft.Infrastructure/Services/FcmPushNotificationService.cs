using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AkaKraft.Infrastructure.Services;

public class FcmPushNotificationService(
    ApplicationDbContext db,
    FirebaseApp firebaseApp,
    ILogger<FcmPushNotificationService> logger) : IPushNotificationService
{
    public async Task SendToUserAsync(Guid userId, string title, string body, string? url = null)
    {
        var tokens = await db.FcmTokens
            .Where(t => t.UserId == userId)
            .Select(t => t.Token)
            .ToListAsync();

        if (tokens.Count == 0) return;

        await SendToTokensAsync(tokens, title, body, url);
    }

    public async Task SendToUsersWithPreferenceAsync(
        Func<UserPreferences, bool> preferenceSelector,
        string title,
        string body,
        string? url = null)
    {
        // AsEnumerable erzwingt Client-seitige Auswertung der Func<>-Prädikat
        var allPrefs = await db.UserPreferences.ToListAsync();
        var userIds = allPrefs
            .Where(preferenceSelector)
            .Select(p => p.UserId)
            .ToList();

        if (userIds.Count == 0) return;

        var tokens = await db.FcmTokens
            .Where(t => userIds.Contains(t.UserId))
            .Select(t => t.Token)
            .ToListAsync();

        if (tokens.Count == 0) return;

        await SendToTokensAsync(tokens, title, body, url);
    }

    private async Task SendToTokensAsync(List<string> tokens, string title, string body, string? url)
    {
        var messaging = FirebaseMessaging.GetMessaging(firebaseApp);

        // FCM erlaubt max. 500 Tokens pro Batch
        foreach (var batch in tokens.Chunk(500))
        {
            var message = new MulticastMessage
            {
                Tokens = batch.ToList(),
                Notification = new Notification { Title = title, Body = body },
                Webpush = new WebpushConfig
                {
                    Notification = new WebpushNotification
                    {
                        Title = title,
                        Body = body,
                        Icon = "/app/android-chrome-192x192.png",
                        Badge = "/app/favicon-32x32.png",
                    },
                    FcmOptions = url is not null
                        ? new WebpushFcmOptions { Link = url }
                        : null,
                },
            };

            try
            {
                var response = await messaging.SendEachForMulticastAsync(message);
                await CleanupInvalidTokensAsync(batch, response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fehler beim Senden von FCM-Nachrichten.");
            }
        }
    }

    private async Task CleanupInvalidTokensAsync(
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
