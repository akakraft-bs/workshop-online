using AkaKraft.Domain.Common;
using AkaKraft.Domain.Enums;

namespace AkaKraft.Domain.Entities;

/// <summary>
/// Belegung eines Parkkontos durch ein Mitglied. Aktiv, solange
/// <see cref="FreigegebenAt"/> null ist und <see cref="AutoExpiresAt"/> in der Zukunft liegt.
/// </summary>
public class ParkClaim : IAuditable
{
    public Guid Id { get; set; }

    public Guid ParkAccountId { get; set; }
    public ParkAccount ParkAccount { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Snapshot des Kennzeichens (normalisiert, Großbuchstaben) zum Zeitpunkt des Check-ins.</summary>
    public string Kennzeichen { get; set; } = string.Empty;

    /// <summary>Snapshot der Fahrzeugbezeichnung "Marke Modell".</summary>
    public string FahrzeugBezeichnung { get; set; } = string.Empty;

    /// <summary>Zeitpunkt der Einfahrt auf den Campus.</summary>
    public DateTime EinfahrtAt { get; set; }

    /// <summary>Optionale Selbsteinschätzung, bis wann das Fahrzeug voraussichtlich steht.</summary>
    public DateTime? VoraussichtlichBis { get; set; }

    /// <summary>Gesetzt, sobald der Nutzer das Konto manuell freigegeben hat.</summary>
    public DateTime? FreigegebenAt { get; set; }

    /// <summary>Harte 24-Stunden-Grenze ab Einfahrt – danach gilt das Konto automatisch als frei.</summary>
    public DateTime AutoExpiresAt { get; set; }

    public ParkBerechtigungArt BerechtigungArt { get; set; }

    /// <summary>Was der Nutzer beim Check-in bestätigt hat (bei Selbstbestätigung / Spontan-Nutzung).</summary>
    public string? BestaetigungHinweis { get; set; }

    /// <summary>Optional verknüpfte Bühne-Halle-1-Kalender-Event-ID.</summary>
    public string? BookingEventId { get; set; }

    public bool Erinnerung2hGesendet { get; set; }
    public bool ErinnerungAblaufGesendet { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
