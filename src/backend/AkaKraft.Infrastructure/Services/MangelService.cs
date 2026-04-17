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
        return await db.Maengel
            .Include(m => m.CreatedBy)
            .Include(m => m.ResolvedBy)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MangelDto(
                m.Id,
                m.Title,
                m.Description,
                m.Kategorie,
                m.Status,
                m.CreatedByUserId,
                db.UserPreferences
                    .Where(p => p.UserId == m.CreatedByUserId && p.DisplayName != null)
                    .Select(p => p.DisplayName!)
                    .FirstOrDefault() ?? m.CreatedBy.Name,
                m.CreatedAt,
                m.ImageUrl,
                m.ResolvedByUserId,
                m.ResolvedByUserId == null ? null :
                    db.UserPreferences
                        .Where(p => p.UserId == m.ResolvedByUserId && p.DisplayName != null)
                        .Select(p => p.DisplayName!)
                        .FirstOrDefault() ?? m.ResolvedBy!.Name,
                m.ResolvedAt,
                m.Note))
            .ToListAsync();
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

        var displayName = await db.UserPreferences
            .Where(p => p.UserId == userId && p.DisplayName != null)
            .Select(p => p.DisplayName!)
            .FirstOrDefaultAsync() ?? mangel.CreatedBy.Name;

        return new MangelDto(
            mangel.Id,
            mangel.Title,
            mangel.Description,
            mangel.Kategorie,
            mangel.Status,
            mangel.CreatedByUserId,
            displayName,
            mangel.CreatedAt,
            mangel.ImageUrl,
            null, null, null, null);
    }

    public async Task<(MangelDto? Dto, bool Forbidden)> ZurueckziehenAsync(Guid id, Guid userId)
    {
        var mangel = await db.Maengel
            .Include(m => m.CreatedBy)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mangel is null)
            return (null, false);

        if (mangel.CreatedByUserId != userId)
            return (null, true);

        if (mangel.Status != MangelStatus.Offen)
            return (null, false);

        mangel.Status = MangelStatus.Zurueckgezogen;
        await db.SaveChangesAsync();

        var displayName = await db.UserPreferences
            .Where(p => p.UserId == mangel.CreatedByUserId && p.DisplayName != null)
            .Select(p => p.DisplayName!)
            .FirstOrDefaultAsync() ?? mangel.CreatedBy.Name;

        return (new MangelDto(
            mangel.Id,
            mangel.Title,
            mangel.Description,
            mangel.Kategorie,
            mangel.Status,
            mangel.CreatedByUserId,
            displayName,
            mangel.CreatedAt,
            mangel.ImageUrl,
            null, null, null, null), false);
    }

    public async Task<MangelDto?> UpdateStatusAsync(Guid id, Guid resolvedByUserId, UpdateMangelStatusDto dto)
    {
        var mangel = await db.Maengel
            .Include(m => m.CreatedBy)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mangel is null)
            return null;

        mangel.Status = dto.Status;
        mangel.Note = dto.Note;
        mangel.ResolvedByUserId = resolvedByUserId;
        mangel.ResolvedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await db.Entry(mangel).Reference(m => m.ResolvedBy).LoadAsync();

        var createdByDisplayName = await db.UserPreferences
            .Where(p => p.UserId == mangel.CreatedByUserId && p.DisplayName != null)
            .Select(p => p.DisplayName!)
            .FirstOrDefaultAsync() ?? mangel.CreatedBy.Name;

        var resolvedByDisplayName = await db.UserPreferences
            .Where(p => p.UserId == resolvedByUserId && p.DisplayName != null)
            .Select(p => p.DisplayName!)
            .FirstOrDefaultAsync() ?? mangel.ResolvedBy!.Name;

        return new MangelDto(
            mangel.Id,
            mangel.Title,
            mangel.Description,
            mangel.Kategorie,
            mangel.Status,
            mangel.CreatedByUserId,
            createdByDisplayName,
            mangel.CreatedAt,
            mangel.ImageUrl,
            mangel.ResolvedByUserId,
            resolvedByDisplayName,
            mangel.ResolvedAt,
            mangel.Note);
    }
}
