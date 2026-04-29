using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class DokumenteService(ApplicationDbContext db, IUploadService uploadService) : IDokumenteService
{
    public async Task<IEnumerable<DokumentOrdnerDto>> GetAllAsync()
    {
        var preferences = await db.UserPreferences
            .Where(p => p.DisplayName != null)
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!);

        var ordner = await db.DokumentOrdner
            .Include(o => o.Dokumente)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        var userIds = ordner
            .SelectMany(o => o.Dokumente.Select(d => d.UploadedByUserId))
            .Distinct()
            .ToHashSet();

        var users = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        return ordner.Select(o => new DokumentOrdnerDto(
            o.Id, o.Name, o.CreatedAt,
            o.Dokumente.OrderBy(d => d.UploadedAt).Select(d =>
            {
                var name = preferences.TryGetValue(d.UploadedByUserId, out var dn) ? dn
                    : users.TryGetValue(d.UploadedByUserId, out var n) ? n
                    : "Unbekannt";
                return new DokumentDto(d.Id, d.FolderId, d.FileName, d.FileUrl, name, d.UploadedAt, d.FileSizeBytes);
            })
        ));
    }

    public async Task<DokumentOrdnerDto> CreateOrdnerAsync(Guid userId, CreateOrdnerDto dto)
    {
        var ordner = new DokumentOrdner
        {
            Id              = Guid.NewGuid(),
            Name            = dto.Name,
            CreatedByUserId = userId,
            CreatedAt       = DateTime.UtcNow,
        };
        db.DokumentOrdner.Add(ordner);
        await db.SaveChangesAsync();
        return new DokumentOrdnerDto(ordner.Id, ordner.Name, ordner.CreatedAt, []);
    }

    public async Task<bool> DeleteOrdnerAsync(Guid id)
    {
        var ordner = await db.DokumentOrdner.Include(o => o.Dokumente).FirstOrDefaultAsync(o => o.Id == id);
        if (ordner is null) return false;

        foreach (var dok in ordner.Dokumente)
            await uploadService.DeleteAsync(dok.FileUrl);

        db.DokumentOrdner.Remove(ordner);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<DokumentDto> CreateDokumentAsync(Guid userId, CreateDokumentDto dto)
    {
        var dok = new Dokument
        {
            Id               = Guid.NewGuid(),
            FolderId         = dto.FolderId,
            FileName         = dto.FileName,
            FileUrl          = dto.FileUrl,
            UploadedByUserId = userId,
            UploadedAt       = DateTime.UtcNow,
            FileSizeBytes    = dto.FileSizeBytes,
        };
        db.Dokumente.Add(dok);
        await db.SaveChangesAsync();

        var displayName = await db.UserPreferences
            .Where(p => p.UserId == userId && p.DisplayName != null)
            .Select(p => p.DisplayName)
            .FirstOrDefaultAsync()
            ?? (await db.Users.FindAsync(userId))?.Name
            ?? "Unbekannt";

        return new DokumentDto(dok.Id, dok.FolderId, dok.FileName, dok.FileUrl, displayName, dok.UploadedAt, dok.FileSizeBytes);
    }

    public async Task<bool> DeleteDokumentAsync(Guid id)
    {
        var dok = await db.Dokumente.FindAsync(id);
        if (dok is null) return false;
        await uploadService.DeleteAsync(dok.FileUrl);
        db.Dokumente.Remove(dok);
        await db.SaveChangesAsync();
        return true;
    }
}
