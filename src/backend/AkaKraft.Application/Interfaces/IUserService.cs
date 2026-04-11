using AkaKraft.Application.DTOs;
using AkaKraft.Domain.Enums;

namespace AkaKraft.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<UserDto?> GetByGoogleIdAsync(string googleId);
    Task<IReadOnlyList<UserDto>> GetAllAsync();
    Task<UserDto> CreateAsync(string googleId, string email, string name, string? pictureUrl);
    Task<UserDto> AssignRoleAsync(Guid userId, Role role);
    Task<UserDto> RemoveRoleAsync(Guid userId, Role role);
}
