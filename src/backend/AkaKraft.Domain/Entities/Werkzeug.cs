namespace AkaKraft.Domain.Entities;

public class Werkzeug
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Dimensions { get; set; }
    public string? StorageLocation { get; set; }
    public bool IsAvailable { get; set; } = true;
    public Guid? BorrowedByUserId { get; set; }
    public User? BorrowedBy { get; set; }
    public DateTime? BorrowedAt { get; set; }
    public DateTime? ExpectedReturnAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
}
