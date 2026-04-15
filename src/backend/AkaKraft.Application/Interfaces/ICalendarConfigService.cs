using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface ICalendarConfigService
{
    Task<IEnumerable<CalendarConfigDto>> GetAllAsync();
    Task<CalendarConfigDto> UpsertAsync(string googleCalendarId, UpdateCalendarConfigDto dto);
}
