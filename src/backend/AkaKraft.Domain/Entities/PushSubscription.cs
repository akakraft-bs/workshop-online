namespace AkaKraft.Domain.Entities;

public class PushSubscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Endpoint { get; set; } = string.Empty;
    public string P256DH { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
