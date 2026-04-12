namespace AkaKraft.Application.DTOs;

public record VerbrauchsmaterialDto(
    Guid Id,
    string Name,
    string Description,
    string Category,
    string Unit,
    int Quantity,
    int? MinQuantity,
    string? ImageUrl
);
