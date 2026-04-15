using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AkaKraft.Infrastructure.Services;

public class AuthService(IUserService userService, IConfiguration configuration) : IAuthService
{
    public async Task<AuthResultDto> HandleGoogleCallbackAsync(
        string googleId, string email, string name, string? pictureUrl)
    {
        var user = await userService.GetByGoogleIdAsync(googleId);

        if (user is null)
        {
            user = await userService.CreateAsync(googleId, email, name, pictureUrl);
        }

        // Admin-E-Mail-Adresse aus der Konfiguration: falls sie übereinstimmt,
        // wird die Admin-Rolle automatisch vergeben (idempotent).
        var adminEmail = configuration["Admin:Email"];
        if (!string.IsNullOrWhiteSpace(adminEmail) &&
            string.Equals(email, adminEmail, StringComparison.OrdinalIgnoreCase))
        {
            await userService.AssignRoleAsync(user.Id, Domain.Enums.Role.Admin);
        }

        var fullUser = await userService.GetByIdAsync(user.Id)
            ?? throw new InvalidOperationException("User not found after creation.");

        var domainUser = new Domain.Entities.User
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
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        return new AuthResultDto(token, expiresAt, fullUser);
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
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole.Role.ToString()));
        }

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
}
