namespace AkaKraft.Domain.Entities;

/// <summary>
/// Einer der beiden von der Uni bereitgestellten Parkraum-Zugänge.
/// Pro Konto darf sich immer nur ein Fahrzeug auf dem Campus befinden.
/// </summary>
public class ParkAccount
{
    public Guid Id { get; set; }

    /// <summary>Anzeigename, z. B. "Parkkonto A".</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Link zum Uni-Portal, in dem das Kennzeichen hinterlegt wird.</summary>
    public string? PortalUrl { get; set; }

    /// <summary>Freitext-Hinweis (z. B. wo die Zugangsdaten liegen).</summary>
    public string? Notiz { get; set; }

    /// <summary>Zugangsdaten für das Bewirtschafter-Portal (von einem Admin gepflegt).</summary>
    public string? PortalUsername { get; set; }
    public string? PortalPassword { get; set; }

    public int SortOrder { get; set; }

    public ICollection<ParkClaim> Claims { get; set; } = new List<ParkClaim>();
}
