namespace AkaKraft.Application.DTOs;

public record UserPreferencesDto(List<string> FavoriteRoutes);

public record UpdateUserPreferencesDto(List<string> FavoriteRoutes);
