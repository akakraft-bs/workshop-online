using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class ProjektService(ApplicationDbContext db, IUploadService uploadService) : IProjektService
{
    public async Task<IEnumerable<ProjektDto>> GetAllAsync()
    {
        var projekte = await db.Projekte
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var userIds = projekte.Select(p => p.CreatedByUserId).Distinct().ToHashSet();

        var preferences = await db.UserPreferences
            .Where(p => userIds.Contains(p.UserId) && p.DisplayName != null)
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!);

        var users = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        return projekte.Select(p => ToDto(p, preferences, users));
    }

    public async Task<ProjektDto> CreateAsync(Guid userId, CreateProjektDto dto)
    {
        var projekt = new Projekt
        {
            Id               = Guid.NewGuid(),
            Name             = dto.Name,
            Description      = dto.Description,
            PlannedStartDate = dto.PlannedStartDate.ToUniversalTime(),
            DurationWeeks    = dto.DurationWeeks,
            ActualStartDate  = dto.ActualStartDate?.ToUniversalTime(),
            Status           = dto.Status,
            ProjektplanUrl   = dto.ProjektplanUrl,
            CreatedByUserId  = userId,
            CreatedAt        = DateTime.UtcNow,
        };

        if (dto.Status == "Abgeschlossen")
            projekt.ActualEndDate = DateTime.UtcNow;

        db.Projekte.Add(projekt);
        await db.SaveChangesAsync();

        var displayName = await ResolveNameAsync(userId);
        return ToDto(projekt, [], new Dictionary<Guid, string> { [userId] = displayName });
    }

    public async Task<ProjektDto?> UpdateAsync(Guid id, UpdateProjektDto dto)
    {
        var projekt = await db.Projekte.FindAsync(id);
        if (projekt is null) return null;

        if (projekt.ProjektplanUrl != dto.ProjektplanUrl)
            await uploadService.DeleteAsync(projekt.ProjektplanUrl);

        projekt.Name             = dto.Name;
        projekt.Description      = dto.Description;
        projekt.PlannedStartDate = dto.PlannedStartDate.ToUniversalTime();
        projekt.DurationWeeks    = dto.DurationWeeks;
        projekt.ActualStartDate  = dto.ActualStartDate?.ToUniversalTime();
        projekt.ProjektplanUrl   = dto.ProjektplanUrl;

        if (dto.Status == "Abgeschlossen" && projekt.Status != "Abgeschlossen")
            projekt.ActualEndDate = DateTime.UtcNow;
        else if (dto.Status != "Abgeschlossen")
            projekt.ActualEndDate = null;

        projekt.Status = dto.Status;

        await db.SaveChangesAsync();

        var displayName = await ResolveNameAsync(projekt.CreatedByUserId);
        return ToDto(projekt, [], new Dictionary<Guid, string> { [projekt.CreatedByUserId] = displayName });
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var projekt = await db.Projekte.FindAsync(id);
        if (projekt is null) return false;
        await uploadService.DeleteAsync(projekt.ProjektplanUrl);
        db.Projekte.Remove(projekt);
        await db.SaveChangesAsync();
        return true;
    }

    private async Task<string> ResolveNameAsync(Guid userId)
    {
        return await db.UserPreferences
            .Where(p => p.UserId == userId && p.DisplayName != null)
            .Select(p => p.DisplayName!)
            .FirstOrDefaultAsync()
            ?? (await db.Users.FindAsync(userId))?.Name
            ?? "Unbekannt";
    }

    private static ProjektDto ToDto(
        Projekt p,
        Dictionary<Guid, string> preferences,
        Dictionary<Guid, string> users)
    {
        var name = preferences.TryGetValue(p.CreatedByUserId, out var dn) ? dn
            : users.TryGetValue(p.CreatedByUserId, out var n) ? n
            : "Unbekannt";

        var baseDate = p.ActualStartDate ?? p.PlannedStartDate;
        var expectedEnd = baseDate.AddDays(p.DurationWeeks * 7);

        return new ProjektDto(
            p.Id, p.Name, p.Description,
            p.PlannedStartDate, p.DurationWeeks,
            p.ActualStartDate, p.ActualEndDate, expectedEnd,
            p.Status, p.ProjektplanUrl, name, p.CreatedAt);
    }
}
