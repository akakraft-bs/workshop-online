using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class HallenbuchService(ApplicationDbContext db) : IHallenbuchService
{
    public async Task<IEnumerable<HallenbuchEintragDto>> GetAllAsync()
    {
        var eintraege = await db.HallenbuchEintraege
            .Include(h => h.User)
            .OrderByDescending(h => h.Start)
            .ToListAsync();

        var userPrefs = await db.UserPreferences
            .Where(p => p.DisplayName != null)
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!);

        return eintraege.Select(h => ToDto(h, userPrefs));
    }

    public async Task<HallenbuchEintragDto> CreateAsync(Guid userId, CreateHallenbuchEintragDto dto)
    {
        var eintrag = new HallenbuchEintrag
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Start = dto.Start.ToUniversalTime(),
            End = dto.End.ToUniversalTime(),
            Description = dto.Description,
            HatGastgeschraubt = dto.HatGastgeschraubt,
            GastschraubenArt = dto.HatGastgeschraubt ? dto.GastschraubenArt : null,
            GastschraubenBezahlt = dto.HatGastgeschraubt ? dto.GastschraubenBezahlt : null,
            CreatedAt = DateTime.UtcNow,
        };

        db.HallenbuchEintraege.Add(eintrag);
        await db.SaveChangesAsync();

        await db.Entry(eintrag).Reference(h => h.User).LoadAsync();

        var prefs = await db.UserPreferences
            .Where(p => p.UserId == userId && p.DisplayName != null)
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!);

        return ToDto(eintrag, prefs);
    }

    public async Task<(HallenbuchEintragDto? Dto, bool Forbidden)> UpdateAsync(
        Guid id, Guid requestingUserId, bool isPrivileged, UpdateHallenbuchEintragDto dto)
    {
        var eintrag = await db.HallenbuchEintraege
            .Include(h => h.User)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (eintrag is null) return (null, false);

        // Mitglieder dürfen nur eigene Einträge, und nur innerhalb von 7 Tagen
        if (!isPrivileged)
        {
            if (eintrag.UserId != requestingUserId) return (null, true);
            if (DateTime.UtcNow - eintrag.CreatedAt > TimeSpan.FromDays(7)) return (null, true);
        }

        eintrag.Start = dto.Start.ToUniversalTime();
        eintrag.End = dto.End.ToUniversalTime();
        eintrag.Description = dto.Description;
        eintrag.HatGastgeschraubt = dto.HatGastgeschraubt;
        eintrag.GastschraubenArt = dto.HatGastgeschraubt ? dto.GastschraubenArt : null;
        eintrag.GastschraubenBezahlt = dto.HatGastgeschraubt ? dto.GastschraubenBezahlt : null;

        await db.SaveChangesAsync();

        var prefs = await db.UserPreferences
            .Where(p => p.UserId == eintrag.UserId && p.DisplayName != null)
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!);

        return (ToDto(eintrag, prefs), false);
    }

    public async Task<(bool Success, bool Forbidden)> DeleteAsync(
        Guid id, Guid requestingUserId, bool isPrivileged)
    {
        var eintrag = await db.HallenbuchEintraege.FindAsync(id);
        if (eintrag is null) return (false, false);

        if (!isPrivileged)
        {
            if (eintrag.UserId != requestingUserId) return (false, true);
            if (DateTime.UtcNow - eintrag.CreatedAt > TimeSpan.FromDays(7)) return (false, true);
        }

        db.HallenbuchEintraege.Remove(eintrag);
        await db.SaveChangesAsync();
        return (true, false);
    }

    public async Task<IEnumerable<HallenbuchStatistikEintragDto>> GetStatistikAsync(DateTime from, DateTime to)
    {
        var fromUtc = from.ToUniversalTime();
        var toUtc = to.ToUniversalTime();

        var eintraege = await db.HallenbuchEintraege
            .Include(h => h.User)
            .Where(h => h.Start >= fromUtc && h.Start <= toUtc)
            .ToListAsync();

        var userPrefs = await db.UserPreferences
            .Where(p => p.DisplayName != null)
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!);

        return eintraege
            .GroupBy(h => h.UserId)
            .Select(g =>
            {
                var user = g.First().User;
                var name = userPrefs.TryGetValue(g.Key, out var n) ? n : user.Name;
                var eigeneStunden = g.Sum(h => (h.End - h.Start).TotalHours);
                var gastStunden = g.Where(h => h.HatGastgeschraubt)
                                   .Sum(h => (h.End - h.Start).TotalHours);
                return new HallenbuchStatistikEintragDto(g.Key, name, eigeneStunden, gastStunden);
            })
            .OrderBy(s => s.UserName);
    }

    private static HallenbuchEintragDto ToDto(HallenbuchEintrag h, Dictionary<Guid, string> prefs)
    {
        var name = prefs.TryGetValue(h.UserId, out var n) ? n : h.User.Name;
        return new HallenbuchEintragDto(
            h.Id, h.UserId, name,
            h.Start, h.End, h.Description,
            h.HatGastgeschraubt, h.GastschraubenArt, h.GastschraubenBezahlt,
            h.CreatedAt);
    }
}
