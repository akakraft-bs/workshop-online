namespace AkaKraft.Application.DTOs;

public record HallenCheckDto(
    Guid Id,
    Guid UserId,
    string DisplayName,
    string? PictureUrl,
    string? Message,
    DateTime CheckedInAt,
    DateTime ExpiresAt
);

public record HallenCheckInRequest(string? Message);
