namespace AkaKraft.Application.DTOs;

public record AuthResultDto(
    string Token,
    DateTime ExpiresAt,
    UserDto User
);
