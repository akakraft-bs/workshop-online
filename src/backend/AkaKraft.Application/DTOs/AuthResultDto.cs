namespace AkaKraft.Application.DTOs;

public record AuthResultDto(
    string Token,
    DateTime ExpiresAt,
    UserDto User
)
{
    // Wird nur beim Refresh gesetzt, nicht im initialen Login-Flow
    public string? RefreshToken { get; init; }
}
