using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class VereinZugangService(ApplicationDbContext db) : IVereinZugangService
{
    public async Task<IEnumerable<VereinZugangDto>> GetAllAsync() =>
        await db.VereinZugaenge
            .OrderBy(z => z.Anbieter)
            .Select(z => new VereinZugangDto(z.Id, z.Anbieter, z.Zugangsdaten))
            .ToListAsync();

    public async Task<VereinZugangDto> CreateAsync(CreateVereinZugangDto dto)
    {
        var zugang = new VereinZugang
        {
            Id           = Guid.NewGuid(),
            Anbieter     = dto.Anbieter.Trim(),
            Zugangsdaten = dto.Zugangsdaten.Trim(),
            CreatedAt    = DateTime.UtcNow,
        };
        db.VereinZugaenge.Add(zugang);
        await db.SaveChangesAsync();
        return new VereinZugangDto(zugang.Id, zugang.Anbieter, zugang.Zugangsdaten);
    }

    public async Task<VereinZugangDto?> UpdateAsync(Guid id, UpdateVereinZugangDto dto)
    {
        var zugang = await db.VereinZugaenge.FindAsync(id);
        if (zugang is null) return null;
        zugang.Anbieter     = dto.Anbieter.Trim();
        zugang.Zugangsdaten = dto.Zugangsdaten.Trim();
        await db.SaveChangesAsync();
        return new VereinZugangDto(zugang.Id, zugang.Anbieter, zugang.Zugangsdaten);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var zugang = await db.VereinZugaenge.FindAsync(id);
        if (zugang is null) return false;
        db.VereinZugaenge.Remove(zugang);
        await db.SaveChangesAsync();
        return true;
    }
}
