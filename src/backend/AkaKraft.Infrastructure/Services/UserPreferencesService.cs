using System.Text.Json;
using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class UserPreferencesService(ApplicationDbContext db) : IUserPreferencesService
{
    public async Task<UserPreferencesDto> GetAsync(Guid userId)
    {
        var prefs = await db.UserPreferences.FindAsync(userId);
        return ToDto(prefs);
    }

    public async Task<UserPreferencesDto> UpdateAsync(Guid userId, UpdateUserPreferencesDto dto)
    {
        var prefs = await db.UserPreferences.FindAsync(userId);

        var json = JsonSerializer.Serialize(dto.FavoriteRoutes ?? []);

        if (prefs is null)
        {
            prefs = new UserPreferences { UserId = userId, FavoriteRoutesJson = json };
            db.UserPreferences.Add(prefs);
        }
        else
        {
            prefs.FavoriteRoutesJson = json;
        }

        await db.SaveChangesAsync();
        return ToDto(prefs);
    }

    private static UserPreferencesDto ToDto(UserPreferences? prefs)
    {
        if (prefs is null)
            return new UserPreferencesDto([]);

        try
        {
            var routes = JsonSerializer.Deserialize<List<string>>(prefs.FavoriteRoutesJson) ?? [];
            return new UserPreferencesDto(routes);
        }
        catch
        {
            return new UserPreferencesDto([]);
        }
    }
}
