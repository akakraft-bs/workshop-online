using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface IMangelService
{
    Task<IEnumerable<MangelDto>> GetAllAsync();
    Task<MangelDto> CreateAsync(Guid userId, CreateMangelDto dto);
    Task<(MangelDto? Dto, bool Forbidden)> ZurueckziehenAsync(Guid id, Guid userId);
    Task<MangelDto?> UpdateStatusAsync(Guid id, Guid resolvedByUserId, UpdateMangelStatusDto dto);
    Task<(MangelDto? Dto, bool Forbidden)> UpdateContentAsync(Guid id, Guid userId, bool isPrivileged, UpdateMangelContentDto dto);

    Task<MangelAnmerkungDto?> AddAnmerkungAsync(Guid mangelId, Guid userId, CreateMangelAnmerkungDto dto);
    Task<(MangelAnmerkungDto? Dto, bool Forbidden)> UpdateAnmerkungAsync(Guid mangelId, Guid anmerkungId, Guid userId, bool isPrivileged, UpdateMangelAnmerkungDto dto);
    Task<(bool Success, bool Forbidden)> DeleteAnmerkungAsync(Guid mangelId, Guid anmerkungId, Guid userId, bool isPrivileged);
}
