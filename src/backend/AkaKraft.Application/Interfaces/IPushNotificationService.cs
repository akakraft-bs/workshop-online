namespace AkaKraft.Application.Interfaces;

public interface IPushNotificationService
{
    /// <summary>
    /// Sendet eine Push-Benachrichtigung an alle Geräte eines Nutzers.
    /// </summary>
    Task SendToUserAsync(Guid userId, string title, string body, string? url = null);

    /// <summary>
    /// Sendet eine Push-Benachrichtigung an alle Nutzer mit der angegebenen Rolle,
    /// bei denen die Einstellung aktiviert ist.
    /// </summary>
    Task SendToUsersWithPreferenceAsync(
        Func<Domain.Entities.UserPreferences, bool> preferenceSelector,
        string title,
        string body,
        string? url = null);
}
