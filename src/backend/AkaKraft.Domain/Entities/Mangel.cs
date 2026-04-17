using AkaKraft.Domain.Enums;

namespace AkaKraft.Domain.Entities;

public class Mangel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MangelKategorie Kategorie { get; set; }
    public MangelStatus Status { get; set; } = MangelStatus.Offen;

    public Guid CreatedByUserId { get; set; }
    public User CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public string? ImageUrl { get; set; }

    public Guid? ResolvedByUserId { get; set; }
    public User? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Note { get; set; }
}
