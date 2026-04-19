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
    IEmailService emailService,
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

    // -------------------------------------------------------------------------
    // E-Mail-Registrierung
    // -------------------------------------------------------------------------

    public async Task<string?> RegisterAsync(RegisterRequest request, string frontendBaseUrl)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        if (existing is not null)
            return "Diese E-Mail-Adresse ist bereits registriert.";

        var passwordHash = HashPassword(request.Password);
        var token = GenerateSecureToken();
        var expiry = DateTime.UtcNow.AddHours(24);
        var displayName = request.DisplayName.Trim();
        var name = displayName; // Name = DisplayName bei Registrierung

        await userService.CreateEmailUserAsync(
            normalizedEmail, name, passwordHash, token, expiry, displayName);

        var link = $"{frontendBaseUrl}/auth/confirm-email?token={Uri.EscapeDataString(token)}";
        await emailService.SendEmailConfirmationAsync(normalizedEmail, name, link);

        return null;
    }

    public async Task<(AuthResultDto? Result, string? Error)> ConfirmEmailAsync(string token)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.EmailConfirmationToken == token);

        if (user is null || user.EmailConfirmationTokenExpiry < DateTime.UtcNow)
            return (null, "Der Bestätigungslink ist ungültig oder abgelaufen.");

        user.EmailConfirmedAt = DateTime.UtcNow;
        user.EmailConfirmationToken = null;
        user.EmailConfirmationTokenExpiry = null;
        await db.SaveChangesAsync();

        var jwt = GenerateJwtToken(user);
        var expiryMinutes = configuration.GetValue<int>("Authentication:Jwt:ExpiryMinutes");
        var userDto = await userService.GetByIdAsync(user.Id)!
            ?? throw new InvalidOperationException("User not found after confirm.");
        return (new AuthResultDto(jwt, DateTime.UtcNow.AddMinutes(expiryMinutes), userDto), null);
    }

    public async Task<(AuthResultDto? Result, string? Error)> LoginAsync(LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user is null || string.IsNullOrEmpty(user.PasswordHash) || !VerifyPassword(request.Password, user.PasswordHash))
            return (null, "invalid_credentials");

        if (user.EmailConfirmedAt is null)
            return (null, "email_not_confirmed");

        var jwt = GenerateJwtToken(user);
        var expiryMinutes = configuration.GetValue<int>("Authentication:Jwt:ExpiryMinutes");
        var userDto = await userService.GetByIdAsync(user.Id)!
            ?? throw new InvalidOperationException("User not found after login.");
        return (new AuthResultDto(jwt, DateTime.UtcNow.AddMinutes(expiryMinutes), userDto), null);
    }

    public async Task ResendConfirmationAsync(string email, string frontendBaseUrl)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user is null || user.EmailConfirmedAt is not null)
            return; // Silently ignore

        var token = GenerateSecureToken();
        user.EmailConfirmationToken = token;
        user.EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(24);
        await db.SaveChangesAsync();

        var link = $"{frontendBaseUrl}/auth/confirm-email?token={Uri.EscapeDataString(token)}";
        await emailService.SendEmailConfirmationAsync(user.Email, user.Name, link);
    }

    // -------------------------------------------------------------------------
    // Passwort zurücksetzen
    // -------------------------------------------------------------------------

    public async Task RequestPasswordResetAsync(string email, string frontendBaseUrl)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        // Immer 200 zurückgeben – nie verraten ob die E-Mail existiert
        if (user is null || !string.IsNullOrEmpty(user.GoogleId))
            return;

        var token = GenerateSecureToken();
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await db.SaveChangesAsync();

        var link = $"{frontendBaseUrl}/auth/reset-password?token={Uri.EscapeDataString(token)}";
        await emailService.SendPasswordResetAsync(user.Email, user.Name, link);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token);

        if (user is null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            return false;

        user.PasswordHash = HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        await db.SaveChangesAsync();

        return true;
    }

    // -------------------------------------------------------------------------
    // Hilfsmethoden
    // -------------------------------------------------------------------------

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, iterations: 350_000,
            HashAlgorithmName.SHA512, outputLength: 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, iterations: 350_000,
            HashAlgorithmName.SHA512, outputLength: 32);
        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }

    private static string GenerateSecureToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
