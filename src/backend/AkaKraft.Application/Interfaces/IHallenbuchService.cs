using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface IHallenbuchService
{
    Task<IEnumerable<HallenbuchEintragDto>> GetAllAsync();
    Task<HallenbuchEintragDto> CreateAsync(Guid userId, CreateHallenbuchEintragDto dto);
    Task<(HallenbuchEintragDto? Dto, bool Forbidden)> UpdateAsync(Guid id, Guid requestingUserId, bool isPrivileged, UpdateHallenbuchEintragDto dto);
    Task<(bool Success, bool Forbidden)> DeleteAsync(Guid id, Guid requestingUserId, bool isPrivileged);
    Task<IEnumerable<HallenbuchStatistikEintragDto>> GetStatistikAsync(DateTime from, DateTime to);
}
