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
                w.Dimensions,
                w.IsAvailable,
                w.BorrowedByUserId,
                w.BorrowedBy != null ? w.BorrowedBy.Name : null,
                w.BorrowedAt,
                w.ExpectedReturnAt,
                w.ReturnedAt))
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync() =>
        await db.Werkzeuge
            .Select(w => w.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

    public async Task<WerkzeugDto> CreateAsync(CreateWerkzeugDto dto)
    {
        var werkzeug = new Werkzeug
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            ImageUrl = dto.ImageUrl,
            Dimensions = dto.Dimensions,
            IsAvailable = true,
        };

        db.Werkzeuge.Add(werkzeug);
        await db.SaveChangesAsync();

        return new WerkzeugDto(
            werkzeug.Id, werkzeug.Name, werkzeug.Description,
            werkzeug.Category, werkzeug.ImageUrl, werkzeug.Dimensions,
            werkzeug.IsAvailable, null, null, null, null, null);
    }

    public async Task<WerkzeugDto?> UpdateAsync(Guid id, UpdateWerkzeugDto dto)
    {
        var werkzeug = await db.Werkzeuge
            .Include(w => w.BorrowedBy)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (werkzeug is null)
            return null;

        // Altes hochgeladenes Bild löschen wenn es ersetzt wird
        if (werkzeug.ImageUrl != dto.ImageUrl)
            await uploadService.DeleteAsync(werkzeug.ImageUrl);

        werkzeug.Name = dto.Name;
        werkzeug.Description = dto.Description;
        werkzeug.Category = dto.Category;
        werkzeug.ImageUrl = dto.ImageUrl;
        werkzeug.Dimensions = dto.Dimensions;

        await db.SaveChangesAsync();

        return ToDto(werkzeug);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var werkzeug = await db.Werkzeuge.FindAsync(id);
        if (werkzeug is null)
            return false;

        await uploadService.DeleteAsync(werkzeug.ImageUrl);
        db.Werkzeuge.Remove(werkzeug);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<WerkzeugDto?> AusleihenAsync(Guid id, Guid userId, DateTime expectedReturnAt)
    {
        var werkzeug = await db.Werkzeuge
            .Include(w => w.BorrowedBy)
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

    private static WerkzeugDto ToDto(Werkzeug w) => new(
        w.Id, w.Name, w.Description, w.Category,
        w.ImageUrl, w.Dimensions, w.IsAvailable,
        w.BorrowedByUserId, w.BorrowedBy?.Name,
        w.BorrowedAt, w.ExpectedReturnAt, w.ReturnedAt);
}
