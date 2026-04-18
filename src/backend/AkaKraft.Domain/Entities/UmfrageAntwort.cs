namespace AkaKraft.Domain.Entities;

/// <summary>Represents a single user's selection of one option in a poll.</summary>
public class UmfrageAntwort
{
    public Guid Id { get; set; }

    public Guid UmfrageId { get; set; }
    public Umfrage Umfrage { get; set; } = null!;

    public Guid OptionId { get; set; }
    public UmfrageOption Option { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime VotedAt { get; set; }
}
