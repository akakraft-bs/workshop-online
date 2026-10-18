using AkaKraft.Domain.Enums;

namespace AkaKraft.Domain.Entities;

public class Feedback
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public string? AppVersion { get; set; }
    /// <summary>Backend-Version zum Zeitpunkt des Feedbacks (kann bei getrennten Deployments von der App-Version abweichen).</summary>
    public string? ServerVersion { get; set; }
    public FeedbackStatus Status { get; set; } = FeedbackStatus.New;
    public DateTime CreatedAt { get; set; }
}
