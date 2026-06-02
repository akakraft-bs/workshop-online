using AkaKraft.Domain.Common;
using AkaKraft.Domain.Enums;

namespace AkaKraft.Domain.Entities;

public class Partner : IAuditable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Kategorie { get; set; }
    public PartnerStatus Status { get; set; } = PartnerStatus.Potentiell;
    public string? Website { get; set; }
    public string? Notizen { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Ansprechpartner> Ansprechpartner { get; set; } = [];
    public ICollection<Kontakteintrag> Kontakteintraege { get; set; } = [];
}
