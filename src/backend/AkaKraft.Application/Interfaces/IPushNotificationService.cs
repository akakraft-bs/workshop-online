namespace AkaKraft.Application.Interfaces;

public interface IPushNotificationService
{
    string GetVapidPublicKey();
    Task SaveSubscriptionAsync(Guid userId, string endpoint, string p256dh, string auth);
    Task RemoveSubscriptionAsync(Guid userId, string endpoint);
    Task NotifyUserAsync(Guid userId, string title, string body, string? url = null);
    Task NotifyUsersWithPreferenceAsync(
        Func<Domain.Entities.NotificationPreferences, bool> preferenceSelector,
        string title,
        string body,
        string? url = null,
        IEnumerable<Guid>? restrictToUserIds = null);
}
