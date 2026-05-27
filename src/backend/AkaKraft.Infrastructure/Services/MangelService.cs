using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Domain.Enums;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class MangelService(ApplicationDbContext db) : IMangelService
{
    public async Task<IEnumerable<MangelDto>> GetAllAsync()
    {
        var maengel = await db.Maengel
            .Include(m => m.CreatedBy)
            .Include(m => m.ResolvedBy)
            .Include(m => m.Anmerkungen.OrderBy(a => a.CreatedAt))
                .ThenInclude(a => a.CreatedBy)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        var prefs = await LoadPrefsAsync();
        return maengel.Select(m => BuildDto(m, prefs));
    }

    public async Task<MangelDto> CreateAsync(Guid userId, CreateMangelDto dto)
    {
        var mangel = new Mangel
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Kategorie = dto.Kategorie,
            Status = MangelStatus.Offen,
            ImageUrl = dto.ImageUrl,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
        };

        db.Maengel.Add(mangel);
        await db.SaveChangesAsync();
        await db.Entry(mangel).Reference(m => m.CreatedBy).LoadAsync();

        var prefs = await LoadPrefsAsync();
        return BuildDto(mangel, prefs);
    }

    public async Task<(MangelDto? Dto, bool Forbidden)> ZurueckziehenAsync(Guid id, Guid userId)
    {
        var mangel = await LoadMangelAsync(id);
        if (mangel is null) return (null, false);
        if (mangel.CreatedByUserId != userId) return (null, true);
        if (mangel.Status != MangelStatus.Offen) return (null, false);

        mangel.Status = MangelStatus.Zurueckgezogen;
        await db.SaveChangesAsync();

        var prefs = await LoadPrefsAsync();
        return (BuildDto(mangel, prefs), false);
    }

    public async Task<(MangelDto? Dto, bool Forbidden)> UpdateContentAsync(
        Guid id, Guid userId, bool isPrivileged, UpdateMangelContentDto dto)
    {
        var mangel = await LoadMangelAsync(id);
        if (mangel is null) return (null, false);
        if (!isPrivileged && mangel.CreatedByUserId != userId) return (null, true);

        mangel.Title = dto.Title;
        mangel.Description = dto.Description;
        mangel.Kategorie = dto.Kategorie;
        mangel.ImageUrl = dto.ImageUrl;
        await db.SaveChangesAsync();

        var prefs = await LoadPrefsAsync();
        return (BuildDto(mangel, prefs), false);
    }

    public async Task<MangelDto?> UpdateStatusAsync(Guid id, Guid resolvedByUserId, UpdateMangelStatusDto dto)
    {
        var mangel = await LoadMangelAsync(id);
        if (mangel is null) return null;

        mangel.Status = dto.Status;
        mangel.Note = dto.Note;
        mangel.ResolvedByUserId = resolvedByUserId;
        mangel.ResolvedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await db.Entry(mangel).Reference(m => m.ResolvedBy).LoadAsync();

        var prefs = await LoadPrefsAsync();
        return BuildDto(mangel, prefs);
    }

    // ---- Anmerkungen --------------------------------------------------------

    public async Task<MangelAnmerkungDto?> AddAnmerkungAsync(
        Guid mangelId, Guid userId, CreateMangelAnmerkungDto dto)
    {
        if (!await db.Maengel.AnyAsync(m => m.Id == mangelId))
            return null;

        var anmerkung = new MangelAnmerkung
        {
            Id = Guid.NewGuid(),
            MangelId = mangelId,
            Text = dto.Text,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
        };

        db.MangelAnmerkungen.Add(anmerkung);
        await db.SaveChangesAsync();
        await db.Entry(anmerkung).Reference(a => a.CreatedBy).LoadAsync();

        var prefs = await LoadPrefsAsync();
        return BuildAnmerkungDto(anmerkung, prefs);
    }

    public async Task<(MangelAnmerkungDto? Dto, bool Forbidden)> UpdateAnmerkungAsync(
        Guid mangelId, Guid anmerkungId, Guid userId, bool isPrivileged, UpdateMangelAnmerkungDto dto)
    {
        var anmerkung = await db.MangelAnmerkungen
            .Include(a => a.CreatedBy)
            .FirstOrDefaultAsync(a => a.Id == anmerkungId && a.MangelId == mangelId);

        if (anmerkung is null) return (null, false);
        if (!isPrivileged && anmerkung.CreatedByUserId != userId) return (null, true);

        anmerkung.Text = dto.Text;
        anmerkung.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var prefs = await LoadPrefsAsync();
        return (BuildAnmerkungDto(anmerkung, prefs), false);
    }

    public async Task<(bool Success, bool Forbidden)> DeleteAnmerkungAsync(
        Guid mangelId, Guid anmerkungId, Guid userId, bool isPrivileged)
    {
        var anmerkung = await db.MangelAnmerkungen
            .FirstOrDefaultAsync(a => a.Id == anmerkungId && a.MangelId == mangelId);

        if (anmerkung is null) return (false, false);
        if (!isPrivileged && anmerkung.CreatedByUserId != userId) return (false, true);

        db.MangelAnmerkungen.Remove(anmerkung);
        await db.SaveChangesAsync();
        return (true, false);
    }

    // ---- Helpers ------------------------------------------------------------

    private async Task<Mangel?> LoadMangelAsync(Guid id) =>
        await db.Maengel
            .Include(m => m.CreatedBy)
            .Include(m => m.ResolvedBy)
            .Include(m => m.Anmerkungen.OrderBy(a => a.CreatedAt))
                .ThenInclude(a => a.CreatedBy)
            .FirstOrDefaultAsync(m => m.Id == id);

    private async Task<Dictionary<Guid, string>> LoadPrefsAsync() =>
        await db.UserPreferences
            .Where(p => p.DisplayName != null)
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!);

    private static string DisplayName(Guid uid, User user, Dictionary<Guid, string> prefs) =>
        prefs.TryGetValue(uid, out var name) ? name : user.Name;

    private static MangelAnmerkungDto BuildAnmerkungDto(
        MangelAnmerkung a, Dictionary<Guid, string> prefs) =>
        new(a.Id, a.Text, a.CreatedByUserId,
            DisplayName(a.CreatedByUserId, a.CreatedBy, prefs),
            a.CreatedAt, a.UpdatedAt);

    private static MangelDto BuildDto(Mangel m, Dictionary<Guid, string> prefs)
    {
        var anmerkungen = m.Anmerkungen
            .OrderBy(a => a.CreatedAt)
            .Select(a => BuildAnmerkungDto(a, prefs))
            .ToList();

        return new MangelDto(
            m.Id, m.Title, m.Description, m.Kategorie, m.Status,
            m.CreatedByUserId, DisplayName(m.CreatedByUserId, m.CreatedBy, prefs),
            m.CreatedAt, m.ImageUrl,
            m.ResolvedByUserId,
            m.ResolvedByUserId.HasValue && m.ResolvedBy is not null
                ? DisplayName(m.ResolvedByUserId.Value, m.ResolvedBy, prefs) : null,
            m.ResolvedAt, m.Note, anmerkungen);
    }
}
