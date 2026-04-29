using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Domain.Enums;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class VereinInfoService(ApplicationDbContext db) : IVereinInfoService
{
    private static readonly (Role Role, string Label)[] RoleOrder =
    [
        (Role.Chairman,         "1. Vorsitzender"),
        (Role.ViceChairman,     "2. Vorsitzender"),
        (Role.Treasurer,        "Kassenwart"),
        (Role.Hallenwart,       "Hallenwart"),
        (Role.Veranstaltungswart, "Veranstaltungswart"),
        (Role.Getraenkewart,    "Getränkewart"),
        (Role.Grillwart,        "Grillwart"),
    ];

    public async Task<VereinInfoDto> GetAsync()
    {
        var vorstandRoles = RoleOrder.Select(r => r.Role).ToArray();

        var userRoles = await db.UserRoles
            .Where(ur => vorstandRoles.Contains(ur.Role))
            .Include(ur => ur.User)
            .ToListAsync();

        var userIds = userRoles.Select(ur => ur.UserId).Distinct().ToList();
        var prefsMap = await db.UserPreferences
            .Where(p => userIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId);

        var amtstraeger = RoleOrder
            .SelectMany(entry => userRoles
                .Where(ur => ur.Role == entry.Role)
                .Select(ur =>
                {
                    prefsMap.TryGetValue(ur.UserId, out var prefs);
                    var name = prefs?.DisplayName ?? ur.User.Name;
                    return new AmtsTraegerDto(
                        entry.Role.ToString(),
                        entry.Label,
                        ur.UserId.ToString(),
                        name,
                        prefs?.Phone,
                        prefs?.Address);
                }))
            .ToList();

        var schluessel = await db.VereinSchluesselhinterlegungen
            .OrderBy(s => s.SortOrder)
            .Select(s => new SchluesselhinterlegungDto(s.Id, s.Name, s.Address, s.Phone, s.SortOrder))
            .ToListAsync();

        return new VereinInfoDto(amtstraeger, schluessel);
    }

    public async Task<SchluesselhinterlegungDto> CreateSchluesselhinterlegungAsync(CreateSchluesselhinterlegungDto dto)
    {
        var maxOrder = await db.VereinSchluesselhinterlegungen.AnyAsync()
            ? await db.VereinSchluesselhinterlegungen.MaxAsync(s => s.SortOrder)
            : -1;

        var entry = new VereinSchluesselhinterlegung
        {
            Id        = Guid.NewGuid(),
            Name      = dto.Name,
            Address   = dto.Address,
            Phone     = dto.Phone,
            SortOrder = maxOrder + 1,
        };
        db.VereinSchluesselhinterlegungen.Add(entry);
        await db.SaveChangesAsync();
        return new SchluesselhinterlegungDto(entry.Id, entry.Name, entry.Address, entry.Phone, entry.SortOrder);
    }

    public async Task<SchluesselhinterlegungDto?> UpdateSchluesselhinterlegungAsync(Guid id, UpdateSchluesselhinterlegungDto dto)
    {
        var entry = await db.VereinSchluesselhinterlegungen.FindAsync(id);
        if (entry is null) return null;
        entry.Name    = dto.Name;
        entry.Address = dto.Address;
        entry.Phone   = dto.Phone;
        await db.SaveChangesAsync();
        return new SchluesselhinterlegungDto(entry.Id, entry.Name, entry.Address, entry.Phone, entry.SortOrder);
    }

    public async Task<bool> DeleteSchluesselhinterlegungAsync(Guid id)
    {
        var entry = await db.VereinSchluesselhinterlegungen.FindAsync(id);
        if (entry is null) return false;
        db.VereinSchluesselhinterlegungen.Remove(entry);
        await db.SaveChangesAsync();
        return true;
    }
}
