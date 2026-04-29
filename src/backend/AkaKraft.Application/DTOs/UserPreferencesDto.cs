namespace AkaKraft.Application.DTOs;

public record UserPreferencesDto(
    List<string> FavoriteRoutes,
    string? DisplayName,
    string? Phone,
    string? Address,
    bool NotifyLeihruckgabe,
    bool NotifyVeranstaltungen,
    bool NotifyMindestbestand,
    bool NotifyUmfragen);

public record UpdateUserPreferencesDto(
    List<string> FavoriteRoutes,
    string? DisplayName,
    string? Phone,
    string? Address,
    bool NotifyLeihruckgabe,
    bool NotifyVeranstaltungen,
    bool NotifyMindestbestand,
    bool NotifyUmfragen);
