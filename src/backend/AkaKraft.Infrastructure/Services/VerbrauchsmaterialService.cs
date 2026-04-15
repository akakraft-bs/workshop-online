using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class VerbrauchsmaterialService(ApplicationDbContext db) : IVerbrauchsmaterialService
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
                v.ImageUrl))
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
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            Unit = dto.Unit,
            Quantity = dto.Quantity,
            MinQuantity = dto.MinQuantity,
            ImageUrl = dto.ImageUrl,
        };

        db.Verbrauchsmaterialien.Add(item);
        await db.SaveChangesAsync();

        return new VerbrauchsmaterialDto(
            item.Id, item.Name, item.Description, item.Category,
            item.Unit, item.Quantity, item.MinQuantity, item.ImageUrl);
    }

    public async Task<VerbrauchsmaterialDto?> UpdateAsync(Guid id, UpdateVerbrauchsmaterialDto dto)
    {
        var item = await db.Verbrauchsmaterialien.FindAsync(id);
        if (item is null) return null;

        item.Name        = dto.Name;
        item.Description = dto.Description;
        item.Category    = dto.Category;
        item.Unit        = dto.Unit;
        item.Quantity    = dto.Quantity;
        item.MinQuantity = dto.MinQuantity;
        item.ImageUrl    = dto.ImageUrl;

        await db.SaveChangesAsync();

        return new VerbrauchsmaterialDto(
            item.Id, item.Name, item.Description, item.Category,
            item.Unit, item.Quantity, item.MinQuantity, item.ImageUrl);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await db.Verbrauchsmaterialien.FindAsync(id);
        if (item is null) return false;

        db.Verbrauchsmaterialien.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }
}
