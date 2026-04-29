namespace AkaKraft.Domain.Entities;

public class VereinZugang
{
    public Guid Id { get; set; }
    public string Anbieter { get; set; } = string.Empty;
    public string Zugangsdaten { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
