namespace AkaKraft.Domain.Entities;

public class Verbrauchsmaterial
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int? MinQuantity { get; set; }
    public string? ImageUrl { get; set; }
}
