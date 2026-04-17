namespace AkaKraft.Domain.Entities;

/// <summary>
/// Speichert einen FCM-Token (Gerät) pro Nutzer für Push-Benachrichtigungen.
/// Ein Nutzer kann mehrere Geräte registriert haben.
/// </summary>
public class FcmToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    /// <summary>
    /// Der FCM-Registrierungstoken des Browsers/Geräts.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Zeitpunkt der Registrierung (UTC).
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
