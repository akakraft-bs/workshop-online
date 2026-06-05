using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface IHallenbuchService
{
    Task<PagedResult<HallenbuchEintragDto>> GetPageAsync(int page, int pageSize);
    Task<HallenbuchEintragDto> CreateAsync(Guid userId, CreateHallenbuchEintragDto dto);
    Task<(HallenbuchEintragDto? Dto, bool Forbidden)> UpdateAsync(Guid id, Guid requestingUserId, bool isPrivileged, UpdateHallenbuchEintragDto dto);
    Task<(bool Success, bool Forbidden)> DeleteAsync(Guid id, Guid requestingUserId, bool isPrivileged);
    Task<IEnumerable<HallenbuchStatistikEintragDto>> GetStatistikAsync(DateTime from, DateTime to);
}
