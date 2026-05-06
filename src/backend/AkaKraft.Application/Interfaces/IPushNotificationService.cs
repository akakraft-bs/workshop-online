namespace AkaKraft.Application.Interfaces;

public interface IPushNotificationService
{
    /// <summary>
    /// Sendet eine Push-Benachrichtigung an alle Geräte eines Nutzers.
    /// </summary>
    Task SendToUserAsync(Guid userId, string title, string body, string? url = null);

    /// <summary>
    /// Sendet eine Push-Benachrichtigung an alle Nutzer mit aktivierten Push-Benachrichtigungen.
    /// </summary>
    Task SendToAllSubscribedAsync(string title, string body, string? url = null);

    /// <summary>
    /// Sendet eine Push-Benachrichtigung an eine bestimmte Menge von Nutzern (sofern Push aktiviert).
    /// </summary>
    Task SendToUsersAsync(IEnumerable<Guid> userIds, string title, string body, string? url = null);
}
