using System.Text.Json;
using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;

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
        var displayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? null : dto.DisplayName.Trim();

        if (prefs is null)
        {
            prefs = new UserPreferences
            {
                UserId = userId,
                FavoriteRoutesJson = json,
                DisplayName = displayName,
            };
            db.UserPreferences.Add(prefs);
        }
        else
        {
            prefs.FavoriteRoutesJson = json;
            prefs.DisplayName = displayName;
        }

        await db.SaveChangesAsync();
        return ToDto(prefs);
    }

    private static UserPreferencesDto ToDto(UserPreferences? prefs)
    {
        if (prefs is null)
            return new UserPreferencesDto([], null);

        try
        {
            var routes = JsonSerializer.Deserialize<List<string>>(prefs.FavoriteRoutesJson) ?? [];
            return new UserPreferencesDto(routes, prefs.DisplayName);
        }
        catch
        {
            return new UserPreferencesDto([], null);
        }
    }
}
