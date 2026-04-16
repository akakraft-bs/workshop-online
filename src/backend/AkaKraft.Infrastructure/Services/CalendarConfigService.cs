using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Domain.Enums;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class CalendarConfigService(ApplicationDbContext db) : ICalendarConfigService
{
    public async Task<IEnumerable<CalendarConfigDto>> GetAllAsync()
    {
        return await db.CalendarConfigs
            .Include(c => c.WriteRoles)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => ToDto(c))
            .ToListAsync();
    }

    public async Task<CalendarConfigDto> UpsertAsync(string googleCalendarId, UpdateCalendarConfigDto dto)
    {
        var config = await db.CalendarConfigs
            .Include(c => c.WriteRoles)
            .FirstOrDefaultAsync(c => c.GoogleCalendarId == googleCalendarId);

        if (config is null)
        {
            config = new CalendarConfig
            {
                Id = Guid.NewGuid(),
                GoogleCalendarId = googleCalendarId,
            };
            db.CalendarConfigs.Add(config);
        }

        config.Name = dto.Name;
        config.Color = dto.Color;
        config.IsVisible = dto.IsVisible;
        config.SortOrder = dto.SortOrder;
        config.CalendarType = Enum.TryParse<CalendarType>(dto.CalendarType, ignoreCase: true, out var ct)
            ? ct : CalendarType.Hallenbelegung;

        // Replace write roles
        db.CalendarWriteRoles.RemoveRange(config.WriteRoles);
        config.WriteRoles.Clear();

        foreach (var roleStr in dto.WriteRoles)
        {
            if (Enum.TryParse<Role>(roleStr, ignoreCase: true, out var role) && role != Role.None)
            {
                config.WriteRoles.Add(new CalendarWriteRole
                {
                    Id = Guid.NewGuid(),
                    CalendarConfigId = config.Id,
                    Role = role
                });
            }
        }

        await db.SaveChangesAsync();
        return ToDto(config);
    }

    private static CalendarConfigDto ToDto(CalendarConfig c) => new(
        c.Id,
        c.GoogleCalendarId,
        c.Name,
        c.Color,
        c.IsVisible,
        c.SortOrder,
        c.CalendarType.ToString(),
        c.WriteRoles.Select(r => r.Role.ToString())
    );
}
