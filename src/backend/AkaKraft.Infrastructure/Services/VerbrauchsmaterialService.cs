using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class VerbrauchsmaterialService(ApplicationDbContext db, IUploadService uploadService) : IVerbrauchsmaterialService
{
    public async Task<IEnumerable<VerbrauchsmaterialDto>> GetAllAsync()
    {
        return await db.Verbrauchsmaterialien
            .OrderBy(v => v.Category)
            .ThenBy(v => v.Name)
            .Select(v => new VerbrauchsmaterialDto(
                v.Id,
                v.Name,
                v.Description,
                v.Category,
                v.Unit,
                v.Quantity,
                v.MinQuantity,
                v.ImageUrl,
                v.ThumbnailUrl,
                v.StorageLocation,
                v.CreatedAt))
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync() =>
        await db.Verbrauchsmaterialien
            .Select(v => v.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

    public async Task<IEnumerable<string>> GetUnitsAsync() =>
        await db.Verbrauchsmaterialien
            .Select(v => v.Unit)
            .Distinct()
            .OrderBy(u => u)
            .ToListAsync();

    public async Task<VerbrauchsmaterialDto> CreateAsync(CreateVerbrauchsmaterialDto dto)
    {
        var item = new Verbrauchsmaterial
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            Category = dto.Category.Trim(),
            Unit = dto.Unit.Trim(),
            Quantity = dto.Quantity,
            MinQuantity = dto.MinQuantity,
            ImageUrl = dto.ImageUrl,
            ThumbnailUrl = dto.ThumbnailUrl,
            StorageLocation = dto.StorageLocation?.Trim(),
        };

        db.Verbrauchsmaterialien.Add(item);
        await db.SaveChangesAsync();

        return new VerbrauchsmaterialDto(
            item.Id, item.Name, item.Description, item.Category,
            item.Unit, item.Quantity, item.MinQuantity, item.ImageUrl, item.ThumbnailUrl, item.StorageLocation,
            item.CreatedAt);
    }

    public async Task<VerbrauchsmaterialDto?> UpdateAsync(Guid id, UpdateVerbrauchsmaterialDto dto)
    {
        var item = await db.Verbrauchsmaterialien.FindAsync(id);
        if (item is null) return null;

        if (item.ImageUrl != dto.ImageUrl)
            await uploadService.DeleteAsync(item.ImageUrl, item.ThumbnailUrl);

        item.Name            = dto.Name.Trim();
        item.Description     = dto.Description?.Trim();
        item.Category        = dto.Category.Trim();
        item.Unit            = dto.Unit.Trim();
        item.Quantity        = dto.Quantity;
        item.MinQuantity     = dto.MinQuantity;
        item.ImageUrl        = dto.ImageUrl;
        item.ThumbnailUrl    = dto.ThumbnailUrl;
        item.StorageLocation = dto.StorageLocation?.Trim();

        await db.SaveChangesAsync();

        return new VerbrauchsmaterialDto(
            item.Id, item.Name, item.Description, item.Category,
            item.Unit, item.Quantity, item.MinQuantity, item.ImageUrl, item.ThumbnailUrl, item.StorageLocation,
            item.CreatedAt);
    }

    public async Task<VerbrauchsmaterialDto?> AdjustQuantityAsync(Guid id, int delta)
    {
        var item = await db.Verbrauchsmaterialien.FindAsync(id);
        if (item is null) return null;

        item.Quantity = Math.Max(0, item.Quantity + delta);
        await db.SaveChangesAsync();

        return new VerbrauchsmaterialDto(
            item.Id, item.Name, item.Description, item.Category,
            item.Unit, item.Quantity, item.MinQuantity, item.ImageUrl, item.ThumbnailUrl, item.StorageLocation,
            item.CreatedAt);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await db.Verbrauchsmaterialien.FindAsync(id);
        if (item is null) return false;

        await uploadService.DeleteAsync(item.ImageUrl, item.ThumbnailUrl);
        db.Verbrauchsmaterialien.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }
}
