using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AkaKraft.Infrastructure.Services;

public class AuthService(
    IUserService userService,
    IConfiguration configuration,
    ApplicationDbContext db) : IAuthService
{
    private static readonly int RefreshTokenExpiryDays = 30;

    public async Task<AuthResultDto> HandleGoogleCallbackAsync(
        string googleId, string email, string name, string? pictureUrl)
    {
        var user = await userService.GetByGoogleIdAsync(googleId);

        if (user is null)
            user = await userService.CreateAsync(googleId, email, name, pictureUrl);

        var adminEmail = configuration["Admin:Email"];
        if (!string.IsNullOrWhiteSpace(adminEmail) &&
            string.Equals(email, adminEmail, StringComparison.OrdinalIgnoreCase))
        {
            await userService.AssignRoleAsync(user.Id, Domain.Enums.Role.Admin);
        }

        var fullUser = await userService.GetByIdAsync(user.Id)
            ?? throw new InvalidOperationException("User not found after creation.");

        var domainUser = new User
        {
            Id = fullUser.Id,
            GoogleId = googleId,
            Email = fullUser.Email,
            Name = fullUser.Name,
            PictureUrl = fullUser.PictureUrl,
            CreatedAt = fullUser.CreatedAt,
            UserRoles = fullUser.Roles.Select(r => new UserRole { Role = r }).ToList(),
        };

        var token = GenerateJwtToken(domainUser);
        var expiryMinutes = configuration.GetValue<int>("Authentication:Jwt:ExpiryMinutes");
        return new AuthResultDto(token, DateTime.UtcNow.AddMinutes(expiryMinutes), fullUser);
    }

    public string GenerateJwtToken(User user)
    {
        var jwtConfig = configuration.GetSection("Authentication:Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var userRole in user.UserRoles)
            claims.Add(new Claim(ClaimTypes.Role, userRole.Role.ToString()));

        var expiryMinutes = configuration.GetValue<int>("Authentication:Jwt:ExpiryMinutes");
        var token = new JwtSecurityToken(
            issuer: jwtConfig["Issuer"],
            audience: jwtConfig["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> CreateRefreshTokenAsync(Guid userId)
    {
        // Alte Tokens dieses Nutzers ablaufen lassen (optional: begrenzen auf N aktive)
        var tokenValue = GenerateSecureToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = tokenValue,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
        });
        await db.SaveChangesAsync();

        return tokenValue;
    }

    public async Task<AuthResultDto?> UseRefreshTokenAsync(string token)
    {
        var stored = await db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == token && !r.IsRevoked);

        if (stored is null || stored.ExpiresAt < DateTime.UtcNow)
            return null;

        // Token-Rotation: altes Token widerrufen
        stored.IsRevoked = true;

        var userDto = await userService.GetByIdAsync(stored.UserId);
        if (userDto is null) return null;

        var domainUser = new User
        {
            Id = userDto.Id,
            Email = userDto.Email,
            Name = userDto.Name,
            PictureUrl = userDto.PictureUrl,
            UserRoles = userDto.Roles.Select(r => new UserRole { Role = r }).ToList(),
        };

        var jwt = GenerateJwtToken(domainUser);

        // Neues Refresh Token ausstellen
        var newTokenValue = GenerateSecureToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = newTokenValue,
            UserId = stored.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
        });

        await db.SaveChangesAsync();

        var expiryMinutes = configuration.GetValue<int>("Authentication:Jwt:ExpiryMinutes");
        return new AuthResultDto(jwt, DateTime.UtcNow.AddMinutes(expiryMinutes), userDto)
        {
            // Neuen Refresh-Token-Wert als Extra mitgeben
            RefreshToken = newTokenValue
        };
    }

    public async Task RevokeRefreshTokenAsync(string token)
    {
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
        if (stored is null) return;
        stored.IsRevoked = true;
        await db.SaveChangesAsync();
    }

    private static string GenerateSecureToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
