namespace AkaKraft.Domain.Entities;

public class UmfrageOption
{
    public Guid Id { get; set; }

    public Guid UmfrageId { get; set; }
    public Umfrage Umfrage { get; set; } = null!;

    public string Text { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<UmfrageAntwort> Antworten { get; set; } = [];
}
