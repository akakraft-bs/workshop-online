using AkaKraft.Application.Interfaces;
using AkaKraft.Infrastructure.Data;
using AkaKraft.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WebPush;

namespace AkaKraft.Infrastructure.Services;

public class PushNotificationService(
    ApplicationDbContext db,
    IOptions<VapidOptions> vapidOptions,
    ILogger<PushNotificationService> logger) : IPushNotificationService
{
    private readonly VapidOptions _vapid = vapidOptions.Value;

    public string GetVapidPublicKey() => _vapid.PublicKey;

    public async Task SaveSubscriptionAsync(Guid userId, string endpoint, string p256dh, string auth)
    {
        var existing = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);

        if (existing is not null)
            return;

        db.PushSubscriptions.Add(new Domain.Entities.PushSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Endpoint = endpoint,
            P256DH = p256dh,
            Auth = auth,
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
    }

    public async Task RemoveSubscriptionAsync(Guid userId, string endpoint)
    {
        var sub = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);

        if (sub is null)
            return;

        db.PushSubscriptions.Remove(sub);
        await db.SaveChangesAsync();
    }

    public async Task NotifyUserAsync(Guid userId, string title, string body, string? url = null)
    {
        var subscriptions = await db.PushSubscriptions
            .Where(s => s.UserId == userId)
            .ToListAsync();

        await SendToSubscriptionsAsync(subscriptions, title, body, url);
    }

    public async Task NotifyUsersWithPreferenceAsync(
        Func<Domain.Entities.NotificationPreferences, bool> preferenceSelector,
        string title,
        string body,
        string? url = null,
        IEnumerable<Guid>? restrictToUserIds = null)
    {
        var prefs = await db.NotificationPreferences.ToListAsync();
        var eligibleUserIds = prefs
            .Where(preferenceSelector)
            .Select(p => p.UserId)
            .ToHashSet();

        if (restrictToUserIds is not null)
            eligibleUserIds.IntersectWith(restrictToUserIds.ToHashSet());

        if (eligibleUserIds.Count == 0)
            return;

        var subscriptions = await db.PushSubscriptions
            .Where(s => eligibleUserIds.Contains(s.UserId))
            .ToListAsync();

        await SendToSubscriptionsAsync(subscriptions, title, body, url);
    }

    private async Task SendToSubscriptionsAsync(
        IEnumerable<Domain.Entities.PushSubscription> subscriptions,
        string title,
        string body,
        string? url)
    {
        var payload = JsonSerializer.Serialize(new { title, body, url });
        var client = new WebPushClient();
        client.SetVapidDetails(_vapid.Subject, _vapid.PublicKey, _vapid.PrivateKey);

        var staleEndpoints = new List<Domain.Entities.PushSubscription>();

        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSub = new PushSubscription(sub.Endpoint, sub.P256DH, sub.Auth);
                await client.SendNotificationAsync(pushSub, payload);
            }
            catch (WebPushException ex) when (ex.StatusCode is
                System.Net.HttpStatusCode.Gone or
                System.Net.HttpStatusCode.NotFound)
            {
                staleEndpoints.Add(sub);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Push-Benachrichtigung an {Endpoint} fehlgeschlagen.", sub.Endpoint);
            }
        }

        if (staleEndpoints.Count > 0)
        {
            db.PushSubscriptions.RemoveRange(staleEndpoints);
            await db.SaveChangesAsync();
        }
    }
}
