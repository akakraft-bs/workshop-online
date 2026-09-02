using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface IParkplatzService
{
    /// <summary>Vollständige Übersicht: Konten-Status, eigene Berechtigung, anstehende Reservierungen.</summary>
    Task<ParkOverviewDto> GetOverviewAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Prüft, auf welcher Grundlage der Nutzer aktuell ein Parkkonto übernehmen darf.</summary>
    Task<ParkBerechtigungDto> GetBerechtigungAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default);
}
