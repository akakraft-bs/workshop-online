using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;

namespace AkaKraft.Infrastructure.Services;

public class NotificationPreferencesService(ApplicationDbContext db) : INotificationPreferencesService
{
    public async Task<NotificationPreferencesDto> GetAsync(Guid userId)
    {
        var prefs = await db.NotificationPreferences.FindAsync(userId);
        return ToDto(prefs);
    }

    public async Task<NotificationPreferencesDto> UpdateAsync(Guid userId, UpdateNotificationPreferencesDto dto)
    {
        var prefs = await db.NotificationPreferences.FindAsync(userId);

        if (prefs is null)
        {
            prefs = new NotificationPreferences { UserId = userId };
            db.NotificationPreferences.Add(prefs);
        }

        prefs.WerkzeugRueckgabe = dto.WerkzeugRueckgabe;
        prefs.Veranstaltungen = dto.Veranstaltungen;
        prefs.VerbrauchsmaterialMindestbestand = dto.VerbrauchsmaterialMindestbestand;

        await db.SaveChangesAsync();
        return ToDto(prefs);
    }

    private static NotificationPreferencesDto ToDto(NotificationPreferences? prefs) =>
        prefs is null
            ? new NotificationPreferencesDto(true, true, false)
            : new NotificationPreferencesDto(
                prefs.WerkzeugRueckgabe,
                prefs.Veranstaltungen,
                prefs.VerbrauchsmaterialMindestbestand);
}
