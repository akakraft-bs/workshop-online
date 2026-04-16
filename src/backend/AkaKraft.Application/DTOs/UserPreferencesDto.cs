namespace AkaKraft.Application.DTOs;

public record UserPreferencesDto(List<string> FavoriteRoutes, string? DisplayName);

public record UpdateUserPreferencesDto(List<string> FavoriteRoutes, string? DisplayName);
