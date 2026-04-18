using AkaKraft.Domain.Enums;

namespace AkaKraft.Domain.Entities;

public class Wunsch
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Link { get; set; }
    public WunschStatus Status { get; set; } = WunschStatus.Offen;

    public Guid CreatedByUserId { get; set; }
    public User CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public Guid? ClosedByUserId { get; set; }
    public User? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? CloseNote { get; set; }

    public ICollection<WunschVote> Votes { get; set; } = [];
}
