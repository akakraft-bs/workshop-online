using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class VerbrauchsmaterialService(ApplicationDbContext db) : IVerbrauchsmaterialService
{
    public async Task<IEnumerable<VerbrauchsmaterialDto>> GetAllAsync()
    {
        return await db.Verbrauchsmaterialien
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
}
