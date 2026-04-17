using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface IMangelService
{
    Task<IEnumerable<MangelDto>> GetAllAsync();
    Task<MangelDto> CreateAsync(Guid userId, CreateMangelDto dto);
    Task<(MangelDto? Dto, bool Forbidden)> ZurueckziehenAsync(Guid id, Guid userId);
    Task<MangelDto?> UpdateStatusAsync(Guid id, Guid resolvedByUserId, UpdateMangelStatusDto dto);
}
