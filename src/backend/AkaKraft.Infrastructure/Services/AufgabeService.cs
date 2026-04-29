using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class AufgabeService(ApplicationDbContext db) : IAufgabeService
{
    public async Task<IEnumerable<AufgabeDto>> GetAllAsync() =>
        await db.Aufgaben
            .Include(a => a.CreatedBy)
            .Include(a => a.AssignedUser)
            .OrderBy(a => a.Status == "Erledigt" ? 1 : 0)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new AufgabeDto(
                a.Id,
                a.Titel,
                a.Beschreibung,
                a.FotoUrl,
                a.Status,
                a.AssignedUserId,
                a.AssignedUserId == null ? null :
                    db.UserPreferences
                        .Where(p => p.UserId == a.AssignedUserId && p.DisplayName != null)
                        .Select(p => p.DisplayName!)
                        .FirstOrDefault() ?? a.AssignedUser!.Name,
                a.AssignedName,
                db.UserPreferences
                    .Where(p => p.UserId == a.CreatedByUserId && p.DisplayName != null)
                    .Select(p => p.DisplayName!)
                    .FirstOrDefault() ?? a.CreatedBy.Name,
                a.CreatedAt))
            .ToListAsync();

    public async Task<AufgabeDto> CreateAsync(Guid creatorId, CreateAufgabeDto dto)
    {
        var aufgabe = new Aufgabe
        {
            Id              = Guid.NewGuid(),
            Titel           = dto.Titel.Trim(),
            Beschreibung    = dto.Beschreibung.Trim(),
            FotoUrl         = dto.FotoUrl,
            Status          = "Neu",
            CreatedByUserId = creatorId,
            CreatedAt       = DateTime.UtcNow,
        };
        db.Aufgaben.Add(aufgabe);
        await db.SaveChangesAsync();

        await db.Entry(aufgabe).Reference(a => a.CreatedBy).LoadAsync();
        var createdByName = await GetDisplayNameAsync(creatorId) ?? aufgabe.CreatedBy.Name;

        return new AufgabeDto(aufgabe.Id, aufgabe.Titel, aufgabe.Beschreibung,
            aufgabe.FotoUrl, aufgabe.Status, null, null, null, createdByName, aufgabe.CreatedAt);
    }

    public async Task<AufgabeDto?> UpdateAsync(Guid id, UpdateAufgabeDto dto)
    {
        var aufgabe = await db.Aufgaben
            .Include(a => a.CreatedBy)
            .Include(a => a.AssignedUser)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (aufgabe is null) return null;

        aufgabe.Titel        = dto.Titel.Trim();
        aufgabe.Beschreibung = dto.Beschreibung.Trim();
        aufgabe.FotoUrl      = dto.FotoUrl;

        aufgabe.AssignedUserId = null;
        aufgabe.AssignedName   = null;

        if (dto.Erledigt)
        {
            aufgabe.Status = "Erledigt";
            if (dto.AssignedUserId.HasValue)
                aufgabe.AssignedUserId = dto.AssignedUserId;
            else if (!string.IsNullOrWhiteSpace(dto.AssignedName))
                aufgabe.AssignedName = dto.AssignedName.Trim();
        }
        else if (dto.AssignedUserId.HasValue)
        {
            aufgabe.AssignedUserId = dto.AssignedUserId;
            aufgabe.Status         = "Zugewiesen";
        }
        else if (!string.IsNullOrWhiteSpace(dto.AssignedName))
        {
            aufgabe.AssignedName = dto.AssignedName.Trim();
            aufgabe.Status       = "Zugewiesen";
        }
        else
        {
            aufgabe.Status = "Neu";
        }

        await db.SaveChangesAsync();

        if (aufgabe.AssignedUserId.HasValue)
            await db.Entry(aufgabe).Reference(a => a.AssignedUser).LoadAsync();

        var assignedDisplayName = aufgabe.AssignedUserId.HasValue
            ? await GetDisplayNameAsync(aufgabe.AssignedUserId.Value) ?? aufgabe.AssignedUser!.Name
            : null;
        var createdByName = await GetDisplayNameAsync(aufgabe.CreatedByUserId) ?? aufgabe.CreatedBy.Name;

        return new AufgabeDto(aufgabe.Id, aufgabe.Titel, aufgabe.Beschreibung, aufgabe.FotoUrl,
            aufgabe.Status, aufgabe.AssignedUserId, assignedDisplayName, aufgabe.AssignedName,
            createdByName, aufgabe.CreatedAt);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var aufgabe = await db.Aufgaben.FindAsync(id);
        if (aufgabe is null) return false;
        db.Aufgaben.Remove(aufgabe);
        await db.SaveChangesAsync();
        return true;
    }

    private Task<string?> GetDisplayNameAsync(Guid userId) =>
        db.UserPreferences
            .Where(p => p.UserId == userId && p.DisplayName != null)
            .Select(p => (string?)p.DisplayName)
            .FirstOrDefaultAsync();
}
