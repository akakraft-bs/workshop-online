using AkaKraft.Domain.Enums;

namespace AkaKraft.Domain.Entities;

public class Feedback
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public FeedbackStatus Status { get; set; } = FeedbackStatus.New;
    public DateTime CreatedAt { get; set; }
}
