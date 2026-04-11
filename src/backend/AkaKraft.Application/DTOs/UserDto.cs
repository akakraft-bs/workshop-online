using AkaKraft.Domain.Enums;

namespace AkaKraft.Application.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string Name,
    string? PictureUrl,
    DateTime CreatedAt,
    IReadOnlyList<Role> Roles
);
