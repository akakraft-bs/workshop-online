using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface IUserPreferencesService
{
    Task<UserPreferencesDto> GetAsync(Guid userId);
    Task<UserPreferencesDto> UpdateAsync(Guid userId, UpdateUserPreferencesDto dto);
}
