using AkaKraft.Application.Interfaces;

namespace AkaKraft.Infrastructure.Services;

/// <summary>
/// Fallback-Implementierung wenn Firebase nicht konfiguriert ist (z. B. lokale Entwicklung).
/// </summary>
public class NoOpPushNotificationService : IPushNotificationService
{
    public Task SendToUserAsync(Guid userId, string title, string body, string? url = null)
        => Task.CompletedTask;

    public Task SendToAllSubscribedAsync(string title, string body, string? url = null)
        => Task.CompletedTask;
}
