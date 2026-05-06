namespace AkaKraft.Application.DTOs;

public record UserPreferencesDto(
    List<string> FavoriteRoutes,
    string? DisplayName,
    string? Phone,
    string? Address);

public record UpdateUserPreferencesDto(
    List<string> FavoriteRoutes,
    string? DisplayName,
    string? Phone,
    string? Address);
