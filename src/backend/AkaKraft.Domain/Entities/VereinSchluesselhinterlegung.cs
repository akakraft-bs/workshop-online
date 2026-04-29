namespace AkaKraft.Domain.Entities;

public class VereinSchluesselhinterlegung
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int SortOrder { get; set; }
}
