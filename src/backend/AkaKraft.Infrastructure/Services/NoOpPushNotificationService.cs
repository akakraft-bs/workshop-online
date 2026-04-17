using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;

namespace AkaKraft.Infrastructure.Services;

/// <summary>
/// Fallback-Implementierung wenn Firebase nicht konfiguriert ist (z. B. lokale Entwicklung).
/// </summary>
public class NoOpPushNotificationService : IPushNotificationService
{
    public Task SendToUserAsync(Guid userId, string title, string body, string? url = null)
        => Task.CompletedTask;

    public Task SendToUsersWithPreferenceAsync(
        Func<UserPreferences, bool> preferenceSelector,
        string title,
        string body,
        string? url = null)
        => Task.CompletedTask;
}
