using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AkaKraft.Infrastructure.Services;

public class WerkzeugService(ApplicationDbContext db, IUploadService uploadService) : IWerkzeugService
{
    public async Task<IEnumerable<WerkzeugDto>> GetAllAsync()
    {
        return await db.Werkzeuge
            .Include(w => w.BorrowedBy)
            .Select(w => new WerkzeugDto(
                w.Id,
                w.Name,
                w.Description,
                w.Category,
                w.ImageUrl,
                w.ThumbnailUrl,
                w.Dimensions,
                w.StorageLocation,
                w.IsAvailable,
                w.BorrowedByUserId,
                w.BorrowedBy != null
                    ? db.UserPreferences
                        .Where(p => p.UserId == w.BorrowedByUserId && p.DisplayName != null)
                        .Select(p => p.DisplayName)
                        .FirstOrDefault() ?? w.BorrowedBy.Name
                    : null,
                w.BorrowedAt,
                w.ExpectedReturnAt,
                w.ReturnedAt,
                w.CreatedAt,
                w.AnleitungDokumentId,
                w.AnleitungDokumentId != null
                    ? db.Dokumente.Where(d => d.Id == w.AnleitungDokumentId).Select(d => d.FileName).FirstOrDefault()
                    : null,
                w.AnleitungDokumentId != null
                    ? db.Dokumente.Where(d => d.Id == w.AnleitungDokumentId).Select(d => d.FileUrl).FirstOrDefault()
                    : null))
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync() =>
        await db.Werkzeuge
            .Select(w => w.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

    public async Task<IEnumerable<StorageLocationDto>> GetStorageLocationsAsync()
    {
        var werkzeugLocations = await db.Werkzeuge
            .Where(w => w.StorageLocation != null && w.StorageLocation != "")
            .Select(w => w.StorageLocation!)
            .ToListAsync();

        var verbrauchsLocations = await db.Verbrauchsmaterialien
            .Where(v => v.StorageLocation != null && v.StorageLocation != "")
            .Select(v => v.StorageLocation!)
            .ToListAsync();

        var allNames = werkzeugLocations
            .Concat(verbrauchsLocations)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(l => l)
            .ToList();

        var colorMap = (await db.Ablageorte.ToListAsync())
            .ToDictionary(a => a.Name, a => a.Color, StringComparer.OrdinalIgnoreCase);

        return allNames.Select(name => new StorageLocationDto(name, colorMap.GetValueOrDefault(name)));
    }

    public async Task<WerkzeugDto> CreateAsync(CreateWerkzeugDto dto)
    {
        var werkzeug = new Werkzeug
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            Category = dto.Category.Trim(),
            ImageUrl = dto.ImageUrl,
            ThumbnailUrl = dto.ThumbnailUrl,
            Dimensions = dto.Dimensions?.Trim(),
            StorageLocation = dto.StorageLocation?.Trim(),
            IsAvailable = true,
            AnleitungDokumentId = dto.AnleitungDokumentId,
        };

        db.Werkzeuge.Add(werkzeug);
        await db.SaveChangesAsync();

        return new WerkzeugDto(
            werkzeug.Id, werkzeug.Name, werkzeug.Description,
            werkzeug.Category, werkzeug.ImageUrl, werkzeug.ThumbnailUrl,
            werkzeug.Dimensions, werkzeug.StorageLocation,
            werkzeug.IsAvailable, null, null, null, null, null,
            werkzeug.CreatedAt, werkzeug.AnleitungDokumentId, null, null);
    }

    public async Task<WerkzeugDto?> UpdateAsync(Guid id, UpdateWerkzeugDto dto)
    {
        var werkzeug = await db.Werkzeuge
            .Include(w => w.BorrowedBy)
            .Include(w => w.AnleitungDokument)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (werkzeug is null)
            return null;

        // Altes hochgeladenes Bild löschen wenn es ersetzt wird
        if (werkzeug.ImageUrl != dto.ImageUrl)
            await uploadService.DeleteAsync(werkzeug.ImageUrl, werkzeug.ThumbnailUrl);

        werkzeug.Name = dto.Name.Trim();
        werkzeug.Description = dto.Description.Trim();
        werkzeug.Category = dto.Category.Trim();
        werkzeug.ImageUrl = dto.ImageUrl;
        werkzeug.ThumbnailUrl = dto.ThumbnailUrl;
        werkzeug.Dimensions = dto.Dimensions?.Trim();
        werkzeug.StorageLocation = dto.StorageLocation?.Trim();
        werkzeug.AnleitungDokumentId = dto.AnleitungDokumentId;

        await db.SaveChangesAsync();

        // Reload AnleitungDokument if ID changed
        if (werkzeug.AnleitungDokumentId != null && werkzeug.AnleitungDokument?.Id != werkzeug.AnleitungDokumentId)
            await db.Entry(werkzeug).Reference(w => w.AnleitungDokument).LoadAsync();

        return ToDto(werkzeug);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var werkzeug = await db.Werkzeuge.FindAsync(id);
        if (werkzeug is null)
            return false;

        await uploadService.DeleteAsync(werkzeug.ImageUrl, werkzeug.ThumbnailUrl);
        db.Werkzeuge.Remove(werkzeug);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<WerkzeugDto?> AusleihenAsync(Guid id, Guid userId, DateTime expectedReturnAt)
    {
        var werkzeug = await db.Werkzeuge
            .Include(w => w.BorrowedBy)
            .Include(w => w.AnleitungDokument)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (werkzeug is null || !werkzeug.IsAvailable)
            return null;

        werkzeug.IsAvailable = false;
        werkzeug.BorrowedByUserId = userId;
        werkzeug.BorrowedAt = DateTime.UtcNow;
        werkzeug.ExpectedReturnAt = expectedReturnAt.ToUniversalTime();
        werkzeug.ReturnedAt = null;

        await db.SaveChangesAsync();
        await db.Entry(werkzeug).Reference(w => w.BorrowedBy).LoadAsync();

        return ToDto(werkzeug);
    }

    public async Task<(WerkzeugDto? Dto, bool Forbidden)> ZurueckgebenAsync(
        Guid id, Guid userId, bool isPrivileged)
    {
        var werkzeug = await db.Werkzeuge
            .Include(w => w.BorrowedBy)
            .Include(w => w.AnleitungDokument)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (werkzeug is null || werkzeug.IsAvailable)
            return (null, false);

        if (!isPrivileged && werkzeug.BorrowedByUserId != userId)
            return (null, true);

        werkzeug.IsAvailable = true;
        werkzeug.ReturnedAt = DateTime.UtcNow;
        werkzeug.BorrowedByUserId = null;
        werkzeug.BorrowedAt = null;
        werkzeug.ExpectedReturnAt = null;

        await db.SaveChangesAsync();

        return (ToDto(werkzeug), false);
    }

    private string? ResolveDisplayName(Werkzeug w)
    {
        if (w.BorrowedBy is null) return null;
        var displayName = db.UserPreferences
            .Where(p => p.UserId == w.BorrowedByUserId && p.DisplayName != null)
            .Select(p => p.DisplayName)
            .FirstOrDefault();
        return displayName ?? w.BorrowedBy.Name;
    }

    private WerkzeugDto ToDto(Werkzeug w) => new(
        w.Id, w.Name, w.Description, w.Category,
        w.ImageUrl, w.ThumbnailUrl, w.Dimensions, w.StorageLocation, w.IsAvailable,
        w.BorrowedByUserId, ResolveDisplayName(w),
        w.BorrowedAt, w.ExpectedReturnAt, w.ReturnedAt, w.CreatedAt,
        w.AnleitungDokumentId, w.AnleitungDokument?.FileName, w.AnleitungDokument?.FileUrl);
}
