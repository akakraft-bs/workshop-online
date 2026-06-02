namespace AkaKraft.Domain.Entities;

/// <summary>Records that a user explicitly abstained from a poll.</summary>
public class UmfrageEnthaltung
{
    public Guid Id { get; set; }

    public Guid UmfrageId { get; set; }
    public Umfrage Umfrage { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime AbstainedAt { get; set; }
}
