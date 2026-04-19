using AkaKraft.Application.DTOs;
using AkaKraft.Domain.Entities;

namespace AkaKraft.Application.Interfaces;

public interface IAuthService
{
    // Google OAuth
    Task<AuthResultDto> HandleGoogleCallbackAsync(string googleId, string email, string name, string? pictureUrl);

    // JWT / Refresh
    string GenerateJwtToken(User user);
    Task<string> CreateRefreshTokenAsync(Guid userId);
    Task<AuthResultDto?> UseRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token);

    // E-Mail-Registrierung
    /// <returns>null wenn erfolgreich; Fehlertext wenn die E-Mail bereits vergeben ist.</returns>
    Task<string?> RegisterAsync(RegisterRequest request, string frontendBaseUrl);

    /// <returns>AuthResultDto bei gültigem Token; null wenn Token ungültig/abgelaufen.</returns>
    Task<(AuthResultDto? Result, string? Error)> ConfirmEmailAsync(string token);

    /// <returns>(Result, Error) – Error ist "invalid_credentials" oder "email_not_confirmed".</returns>
    Task<(AuthResultDto? Result, string? Error)> LoginAsync(LoginRequest request);

    Task ResendConfirmationAsync(string email, string frontendBaseUrl);

    // Passwort zurücksetzen (gibt niemals preis ob die E-Mail existiert)
    Task RequestPasswordResetAsync(string email, string frontendBaseUrl);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
}
