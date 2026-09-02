using System.ComponentModel.DataAnnotations.Schema;
using AkaKraft.Domain.Common;

namespace AkaKraft.Domain.Entities;

/// <summary>
/// Ein von einem Nutzer hinterlegtes Fahrzeug – wird beim Parkkonto-Check-in verwendet.
/// </summary>
public class Fahrzeug : IAuditable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Marke { get; set; } = string.Empty;
    public string? Modell { get; set; }
    public string Kennzeichen { get; set; } = string.Empty;

    /// <summary>Standardfahrzeug wird beim Check-in vorausgewählt.</summary>
    public bool IstStandard { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Anzeige-Snapshot, z. B. "VW Golf · BS-XX 123".</summary>
    [NotMapped]
    public string Anzeige => string.IsNullOrWhiteSpace(Modell)
        ? $"{Marke} · {Kennzeichen}"
        : $"{Marke} {Modell} · {Kennzeichen}";
}
