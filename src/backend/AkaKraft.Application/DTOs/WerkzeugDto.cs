namespace AkaKraft.Application.DTOs;

public record WerkzeugDto(
    Guid Id,
    string Name,
    string Description,
    string Category,
    string? ImageUrl,
    string? Dimensions,
    bool IsAvailable,
    Guid? BorrowedByUserId,
    string? BorrowedByName,
    DateTime? BorrowedAt
);
