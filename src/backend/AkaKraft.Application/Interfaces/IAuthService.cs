using AkaKraft.Application.DTOs;
using AkaKraft.Domain.Entities;

namespace AkaKraft.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResultDto> HandleGoogleCallbackAsync(string googleId, string email, string name, string? pictureUrl);
    string GenerateJwtToken(User user);
    Task<string> CreateRefreshTokenAsync(Guid userId);
    Task<AuthResultDto?> UseRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token);
}
