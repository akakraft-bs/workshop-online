namespace AkaKraft.Domain.Entities;

public class HallenCheck
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string? Message { get; set; }
    public DateTime CheckedInAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
