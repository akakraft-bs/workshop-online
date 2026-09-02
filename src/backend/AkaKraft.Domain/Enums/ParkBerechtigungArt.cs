namespace AkaKraft.Domain.Enums;

/// <summary>
/// Auf welcher Grundlage ein Parkkonto übernommen wurde.
/// </summary>
public enum ParkBerechtigungArt
{
    /// <summary>Eine Bühne-Halle-1-Reservierung konnte dem Nutzer sicher zugeordnet werden.</summary>
    Automatisch,

    /// <summary>Es gibt aktuell eine Bühne-Reservierung, die dem Nutzer aber nicht sicher zugeordnet werden konnte – Selbstbestätigung.</summary>
    Selbstbestaetigt,

    /// <summary>Aktuell keine Bühne-Reservierung – spontane Nutzung eines freien Kontos.</summary>
    Spontan,
}
