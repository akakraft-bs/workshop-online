using AkaKraft.Domain.Common;
using AkaKraft.Domain.Enums;

namespace AkaKraft.Domain.Entities;

public class Kontakteintrag : IAuditable
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public Partner Partner { get; set; } = null!;
    public Guid? AnsprechpartnerId { get; set; }
    public Ansprechpartner? Ansprechpartner { get; set; }
    public DateTime Datum { get; set; }
    public KontaktKanal Kanal { get; set; }
    public KontaktReaktion Reaktion { get; set; }
    public string Zusammenfassung { get; set; } = string.Empty;
    public string? NaechsteSchritte { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
