using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface IParkKennzeichenService
{
    /// <summary>Maximale Anzahl Kennzeichen pro Parkkonto (Vorgabe des Portals).</summary>
    const int MaxKennzeichen = 5;

    Task<ParkKennzeichenListeDto> GetAsync(Guid accountId, CancellationToken ct = default);

    Task<ParkKennzeichenListeDto> AddAsync(Guid accountId, Guid userId, string kennzeichen, CancellationToken ct = default);

    Task<ParkKennzeichenListeDto> RemoveAsync(Guid accountId, Guid userId, string kennzeichen, CancellationToken ct = default);

    Task<IReadOnlyList<ParkKennzeichenAuditDto>> GetHistorieAsync(Guid accountId, int limit, CancellationToken ct = default);
}
