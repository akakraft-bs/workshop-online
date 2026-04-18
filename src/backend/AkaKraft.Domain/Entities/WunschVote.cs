namespace AkaKraft.Domain.Entities;

public class WunschVote
{
    public Guid Id { get; set; }
    public Guid WunschId { get; set; }
    public Wunsch Wunsch { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public bool IsUpvote { get; set; }
    public DateTime VotedAt { get; set; }
}
