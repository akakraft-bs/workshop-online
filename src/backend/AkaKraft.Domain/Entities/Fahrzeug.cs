using System.ComponentModel.DataAnnotations.Schema;
using AkaKraft.Domain.Common;
using AkaKraft.Domain.Enums;

namespace AkaKraft.Domain.Entities;

/// <summary>
/// Ein von einem Nutzer hinterlegtes Fahrzeug – wird beim Parkkonto-Check-in verwendet.
/// </summary>
public class Fahrzeug : IAuditable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public FahrzeugArt Art { get; set; } = FahrzeugArt.Pkw;

    public string Marke { get; set; } = string.Empty;
    public string? Modell { get; set; }
    /// <summary>Pflicht außer bei <see cref="FahrzeugArt.Fahrrad"/>; dort ggf. leerer String.</summary>
    public string Kennzeichen { get; set; } = string.Empty;

    /// <summary>Standardfahrzeug wird beim Check-in vorausgewählt.</summary>
    public bool IstStandard { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Leer bei Fahrzeugen ohne Kennzeichenpflicht (Fahrrad).</summary>
    [NotMapped]
    public bool HatKennzeichen => !string.IsNullOrWhiteSpace(Kennzeichen);

    /// <summary>Anzeige-Snapshot, z. B. "VW Golf · BS-XX 123" bzw. "Cube Attention" (ohne Kennzeichen).</summary>
    [NotMapped]
    public string Anzeige
    {
        get
        {
            var name = string.IsNullOrWhiteSpace(Modell) ? Marke : $"{Marke} {Modell}";
            return HatKennzeichen ? $"{name} · {Kennzeichen}" : name;
        }
    }
}
