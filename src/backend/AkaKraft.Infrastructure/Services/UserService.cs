using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Domain.Enums;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.Infrastructure.Services;

public class UserService(ApplicationDbContext db) : IUserService
{
    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null) return null;
        var displayName = await db.UserPreferences
            .Where(p => p.UserId == user.Id)
            .Select(p => p.DisplayName)
            .FirstOrDefaultAsync();
        return MapToDto(user, displayName);
    }

    public async Task<UserDto?> GetByGoogleIdAsync(string googleId)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.GoogleId == googleId);

        if (user is null) return null;
        var displayName = await db.UserPreferences
            .Where(p => p.UserId == user.Id)
            .Select(p => p.DisplayName)
            .FirstOrDefaultAsync();
        return MapToDto(user, displayName);
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null) return null;
        var displayName = await db.UserPreferences
            .Where(p => p.UserId == user.Id)
            .Select(p => p.DisplayName)
            .FirstOrDefaultAsync();
        return MapToDto(user, displayName);
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync()
    {
        return await db.Users
            .Include(u => u.UserRoles)
            .Select(u => new UserDto(
                u.Id,
                u.Email,
                u.Name,
                db.UserPreferences
                    .Where(p => p.UserId == u.Id)
                    .Select(p => p.DisplayName)
                    .FirstOrDefault(),
                u.PictureUrl,
                u.CreatedAt,
                u.UserRoles.Select(ur => ur.Role).ToList()
            ))
            .ToListAsync();
    }

    public async Task<UserDto> AssignRoleAsync(Guid userId, Role role)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        if (!user.UserRoles.Any(ur => ur.Role == role))
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = userId,
                Role = role,
                AssignedAt = DateTime.UtcNow,
            });

            await db.SaveChangesAsync();
        }

        var displayName = await db.UserPreferences
            .Where(p => p.UserId == user.Id)
            .Select(p => p.DisplayName)
            .FirstOrDefaultAsync();
        return MapToDto(user, displayName);
    }

    public async Task<UserDto> RemoveRoleAsync(Guid userId, Role role)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        var userRole = user.UserRoles.FirstOrDefault(ur => ur.Role == role);
        if (userRole is not null)
        {
            user.UserRoles.Remove(userRole);
            await db.SaveChangesAsync();
        }

        var displayName = await db.UserPreferences
            .Where(p => p.UserId == user.Id)
            .Select(p => p.DisplayName)
            .FirstOrDefaultAsync();
        return MapToDto(user, displayName);
    }

    public async Task<UserDto> CreateAsync(string googleId, string email, string name, string? pictureUrl)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            GoogleId = googleId,
            Email = email,
            Name = name,
            PictureUrl = pictureUrl,
            CreatedAt = DateTime.UtcNow,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return MapToDto(user, null);
    }

    private static UserDto MapToDto(User user, string? displayName) =>
        new(
            user.Id,
            user.Email,
            user.Name,
            displayName,
            user.PictureUrl,
            user.CreatedAt,
            user.UserRoles.Select(ur => ur.Role).ToList()
        );
}
