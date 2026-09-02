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
        config.GrantsParkplatzBerechtigung = dto.GrantsParkplatzBerechtigung;

        // Schreibrollen differenziell anpassen: nur wirklich entfernte löschen,
        // nur neue hinzufügen. Unveränderte Rollen bleiben mit ihrer PK bestehen.
        // Wichtig: neue Rollen über db.CalendarWriteRoles.Add hinzufügen – beim
        // Anfügen an config.WriteRoles würde EF Core wegen des gesetzten Guid-Keys
        // (ValueGeneratedOnAdd) fälschlich State = Modified annehmen → UPDATE mit
        // nicht existierender ID → "0 rows affected".
        var gewuenschteRollen = dto.WriteRoles
            .Select(r => Enum.TryParse<Role>(r, ignoreCase: true, out var role) ? role : Role.None)
            .Where(r => r != Role.None)
            .ToHashSet();

        foreach (var r in config.WriteRoles.Where(r => !gewuenschteRollen.Contains(r.Role)).ToList())
        {
            config.WriteRoles.Remove(r);
            db.CalendarWriteRoles.Remove(r);
        }

        var vorhandeneRollen = config.WriteRoles.Select(r => r.Role).ToHashSet();
        foreach (var role in gewuenschteRollen.Where(r => !vorhandeneRollen.Contains(r)))
        {
            // Nur über den DbSet anfügen; EF-Fixup trägt die Rolle selbst in
            // config.WriteRoles ein. Ein zusätzliches config.WriteRoles.Add würde
            // dieselbe Instanz doppelt in die Antwort-DTO bringen.
            db.CalendarWriteRoles.Add(new CalendarWriteRole
            {
                Id = Guid.NewGuid(),
                CalendarConfigId = config.Id,
                Role = role,
            });
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
        c.GrantsParkplatzBerechtigung,
        c.WriteRoles.Select(r => r.Role.ToString())
    );
}
