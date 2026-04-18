using AkaKraft.Domain.Enums;

namespace AkaKraft.Domain.Entities;

public class Umfrage
{
    public Guid Id { get; set; }

    public string Question { get; set; } = string.Empty;

    /// <summary>Whether multiple options can be selected.</summary>
    public bool IsMultipleChoice { get; set; }

    /// <summary>Whether results (vote counts + voter names) are publicly visible.</summary>
    public bool ResultsVisible { get; set; } = true;

    /// <summary>Only relevant when ResultsVisible=false: reveal results after closing.</summary>
    public bool RevealAfterClose { get; set; }

    /// <summary>Optional deadline until which answers are accepted (UTC).</summary>
    public DateTime? Deadline { get; set; }

    /// <summary>Set when the 1-hour deadline reminder notification has been sent.</summary>
    public DateTime? DeadlineReminderSentAt { get; set; }

    public UmfrageStatus Status { get; set; } = UmfrageStatus.Offen;

    public Guid CreatedByUserId { get; set; }
    public User CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public Guid? ClosedByUserId { get; set; }
    public User? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ICollection<UmfrageOption> Options { get; set; } = [];
    public ICollection<UmfrageAntwort> Antworten { get; set; } = [];
}
