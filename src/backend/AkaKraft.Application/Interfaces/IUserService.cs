using AkaKraft.Application.DTOs;
using AkaKraft.Domain.Enums;

namespace AkaKraft.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<UserDto?> GetByGoogleIdAsync(string googleId);
    Task<UserDto?> GetByEmailAsync(string email);
    Task<IReadOnlyList<UserDto>> GetAllAsync();
    Task<UserDto> CreateAsync(string googleId, string email, string name, string? pictureUrl);
    Task<UserDto> CreateEmailUserAsync(string email, string name, string passwordHash, string confirmationToken, DateTime confirmationTokenExpiry, string? displayName);
    Task<UserDto> AssignRoleAsync(Guid userId, Role role);
    Task<UserDto> RemoveRoleAsync(Guid userId, Role role);
}
