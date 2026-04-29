namespace AkaKraft.Domain.Entities;

public class DokumentOrdner
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<Dokument> Dokumente { get; set; } = [];
}
