using AkaKraft.Domain.Common;

namespace AkaKraft.Domain.Entities;

public class Ablageort : IAuditable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
