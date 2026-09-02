namespace AkaKraft.Domain.Entities;

public enum ParkKennzeichenAktion
{
    Hinzugefuegt,
    Entfernt,
}

/// <summary>
/// Protokolliert jede Kennzeichen-Änderung an einem Parkkonto (wer, was, wann).
/// </summary>
public class ParkKennzeichenAudit
{
    public Guid Id { get; set; }

    public Guid ParkAccountId { get; set; }
    public ParkAccount ParkAccount { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Anzeigename des ausführenden Nutzers zum Zeitpunkt der Aktion.</summary>
    public string AusgefuehrtVon { get; set; } = string.Empty;

    public ParkKennzeichenAktion Aktion { get; set; }

    /// <summary>Betroffenes Kennzeichen (normalisiert).</summary>
    public string Kennzeichen { get; set; } = string.Empty;

    /// <summary>Resultierende Kennzeichenliste nach der Aktion (kommagetrennt).</summary>
    public string KennzeichenNachher { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
