namespace AkaKraft.Domain.Entities;

/// <summary>Ein einzelner Ausleih-Vorgang eines Werkzeugs – bildet die Historie.</summary>
public class WerkzeugAusleihe
{
    public Guid Id { get; set; }

    public Guid WerkzeugId { get; set; }
    public Werkzeug? Werkzeug { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public DateTime BorrowedAt { get; set; }
    public DateTime ExpectedReturnAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
}
