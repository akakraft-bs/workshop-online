namespace AkaKraft.Application.DTOs;

public record UserPreferencesDto(
    List<string> FavoriteRoutes,
    string? DisplayName,
    bool NotifyLeihruckgabe,
    bool NotifyVeranstaltungen,
    bool NotifyMindestbestand);

public record UpdateUserPreferencesDto(
    List<string> FavoriteRoutes,
    string? DisplayName,
    bool NotifyLeihruckgabe,
    bool NotifyVeranstaltungen,
    bool NotifyMindestbestand);
