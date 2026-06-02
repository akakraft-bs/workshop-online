using AkaKraft.Domain.Common;

namespace AkaKraft.Domain.Entities;

public class Ansprechpartner : IAuditable
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public Partner Partner { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Email { get; set; }
    public string? Telefon { get; set; }
    public string? Notizen { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
