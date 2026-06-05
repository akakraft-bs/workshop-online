using AkaKraft.Domain.Common;

namespace AkaKraft.Domain.Entities;

public class Werkzeug : IAuditable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Dimensions { get; set; }
    public string? StorageLocation { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsAvailable { get; set; } = true;
    public Guid? BorrowedByUserId { get; set; }
    public User? BorrowedBy { get; set; }
    public DateTime? BorrowedAt { get; set; }
    public DateTime? ExpectedReturnAt { get; set; }
    public DateTime? ReturnedAt { get; set; }

    public Guid? AnleitungDokumentId { get; set; }
    public Dokument? AnleitungDokument { get; set; }
}
