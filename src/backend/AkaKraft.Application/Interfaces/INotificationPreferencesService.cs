using AkaKraft.Application.DTOs;

namespace AkaKraft.Application.Interfaces;

public interface INotificationPreferencesService
{
    Task<NotificationPreferencesDto> GetAsync(Guid userId);
    Task<NotificationPreferencesDto> UpdateAsync(Guid userId, UpdateNotificationPreferencesDto dto);
}
