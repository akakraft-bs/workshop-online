using AkaKraft.Domain.Common;

namespace AkaKraft.Domain.Entities;

public class MangelAnmerkung : IAuditable
{
    public Guid Id { get; set; }
    public Guid MangelId { get; set; }
    public Mangel Mangel { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public User CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
