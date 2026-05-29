using AkaKraft.Domain.Common;
using AkaKraft.Domain.Enums;

namespace AkaKraft.Domain.Entities;

public class Umfrage : IAuditable
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
    public DateTime? UpdatedAt { get; set; }

    public Guid? ClosedByUserId { get; set; }
    public User? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }

    /// <summary>Google Calendar event ID of the linked Veranstaltung.</summary>
    public string? LinkedEventId { get; set; }
    /// <summary>Google Calendar ID of the linked Veranstaltung.</summary>
    public string? LinkedCalendarId { get; set; }
    /// <summary>Denormalized title of the linked event for display.</summary>
    public string? LinkedEventTitle { get; set; }
    /// <summary>Denormalized start date of the linked event (UTC).</summary>
    public DateTime? LinkedEventStart { get; set; }

    /// <summary>Optional description shown below the question.</summary>
    public string? Description { get; set; }

    public ICollection<UmfrageOption> Options { get; set; } = [];
    public ICollection<UmfrageAntwort> Antworten { get; set; } = [];
}
