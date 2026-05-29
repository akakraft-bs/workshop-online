namespace AkaKraft.Application.DTOs;

public record WerkzeugDto(
    Guid Id,
    string Name,
    string Description,
    string Category,
    string? ImageUrl,
    string? ThumbnailUrl,
    string? Dimensions,
    string? StorageLocation,
    bool IsAvailable,
    Guid? BorrowedByUserId,
    string? BorrowedByName,
    DateTime? BorrowedAt,
    DateTime? ExpectedReturnAt,
    DateTime? ReturnedAt
);
