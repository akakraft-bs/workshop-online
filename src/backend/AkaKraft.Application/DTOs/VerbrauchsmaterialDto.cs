namespace AkaKraft.Application.DTOs;

public record VerbrauchsmaterialDto(
    Guid Id,
    string Name,
    string Description,
    string Category,
    string Unit,
    int Quantity,
    int? MinQuantity,
    string? ImageUrl,
    string? ThumbnailUrl,
    string? StorageLocation,
    DateTime CreatedAt,
    bool IsNachbestellt,
    string? NachbestelltVonName,
    DateTime? NachbestelltAt
);

public record CreateVerbrauchsmaterialDto(
    string Name,
    string Description,
    string Category,
    string Unit,
    int Quantity,
    int? MinQuantity,
    string? ImageUrl,
    string? ThumbnailUrl,
    string? StorageLocation
);

public record UpdateVerbrauchsmaterialDto(
    string Name,
    string Description,
    string Category,
    string Unit,
    int Quantity,
    int? MinQuantity,
    string? ImageUrl,
    string? ThumbnailUrl,
    string? StorageLocation
);

public record AdjustQuantityDto(int Delta);
