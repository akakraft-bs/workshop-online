using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class WerkzeugService(ApplicationDbContext db) : IWerkzeugService
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
                w.BorrowedAt))
            .ToListAsync();
    }

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
            werkzeug.Id,
            werkzeug.Name,
            werkzeug.Description,
            werkzeug.Category,
            werkzeug.ImageUrl,
            werkzeug.Dimensions,
            werkzeug.IsAvailable,
            null, null, null);
    }

    public async Task<WerkzeugDto?> UpdateAsync(Guid id, UpdateWerkzeugDto dto)
    {
        var werkzeug = await db.Werkzeuge
            .Include(w => w.BorrowedBy)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (werkzeug is null)
            return null;

        werkzeug.Name = dto.Name;
        werkzeug.Description = dto.Description;
        werkzeug.Category = dto.Category;
        werkzeug.ImageUrl = dto.ImageUrl;
        werkzeug.Dimensions = dto.Dimensions;

        await db.SaveChangesAsync();

        return new WerkzeugDto(
            werkzeug.Id,
            werkzeug.Name,
            werkzeug.Description,
            werkzeug.Category,
            werkzeug.ImageUrl,
            werkzeug.Dimensions,
            werkzeug.IsAvailable,
            werkzeug.BorrowedByUserId,
            werkzeug.BorrowedBy?.Name,
            werkzeug.BorrowedAt);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var werkzeug = await db.Werkzeuge.FindAsync(id);
        if (werkzeug is null)
            return false;

        db.Werkzeuge.Remove(werkzeug);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<WerkzeugDto?> AusleihenAsync(Guid id, Guid userId)
    {
        var werkzeug = await db.Werkzeuge
            .Include(w => w.BorrowedBy)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (werkzeug is null || !werkzeug.IsAvailable)
            return null;

        werkzeug.IsAvailable = false;
        werkzeug.BorrowedByUserId = userId;
        werkzeug.BorrowedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await db.Entry(werkzeug).Reference(w => w.BorrowedBy).LoadAsync();

        return new WerkzeugDto(
            werkzeug.Id,
            werkzeug.Name,
            werkzeug.Description,
            werkzeug.Category,
            werkzeug.ImageUrl,
            werkzeug.Dimensions,
            werkzeug.IsAvailable,
            werkzeug.BorrowedByUserId,
            werkzeug.BorrowedBy?.Name,
            werkzeug.BorrowedAt);
    }
}
