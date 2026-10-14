using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Enums;
using AkaKraft.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace AkaKraft.WebApi.Endpoints;

internal static class AuthEndpoints
{
    // -------------------------------------------------------------------------
    // Auth Endpoints
    // -------------------------------------------------------------------------
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        // Schritt 1: Frontend leitet hierher weiter → Google-Login startet.
        // Die iOS-App übergibt zusätzlich client=ios und eine PKCE-Challenge (S256).
        app.MapGet("/auth/login/google", (HttpContext ctx, string? client, string? code_challenge) =>
        {
            var properties = new AuthenticationProperties { RedirectUri = $"{ctx.Request.PathBase}/auth/callback" };
            if (client == MobileClient)
            {
                if (!IsValidPkceChallenge(code_challenge))
                    return Results.BadRequest(new { error = "Ungültige PKCE-Challenge." });
                properties.Items[ClientItem] = MobileClient;
                properties.Items[ChallengeItem] = code_challenge;
            }
            return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
        });

        // Schritt 2: Google-Callback → Nutzer anlegen/laden → JWT + Refresh-Token erzeugen → Frontend weiterleiten
        app.MapGet("/auth/callback", async (HttpContext ctx, IAuthService authService, IConfiguration config, IMemoryCache cache) =>
        {
            var result = await ctx.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return Results.Unauthorized();

            var principal = result.Principal;
            var googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var email    = principal.FindFirstValue(ClaimTypes.Email)!;
            var name     = principal.FindFirstValue(ClaimTypes.Name)!;
            var picture  = principal.FindFirstValue("picture");

            var authResult = await authService.HandleGoogleCallbackAsync(googleId, email, name, picture);

            // iOS-App: Kein Token in der URL, sondern ein einmaliger Code, der nur mit dem
            // passenden PKCE-Verifier eingelöst werden kann.
            if (result.Properties?.Items.TryGetValue(ClientItem, out var client) == true && client == MobileClient
                && result.Properties.Items.TryGetValue(ChallengeItem, out var challenge) && challenge is not null)
            {
                await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var code = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
                cache.Set(MobileCodeKey(code), new MobileLoginGrant(authResult.User.Id, challenge, authResult.Token, authResult.ExpiresAt), TimeSpan.FromMinutes(2));
                return Results.Redirect($"{MobileCallbackUrl}?code={code}");
            }

            var refreshToken = await authService.CreateRefreshTokenAsync(authResult.User.Id);

            ctx.Response.Cookies.Append("refresh_token", refreshToken, RefreshCookieOptions(ctx));

            var frontendUrl = config["Frontend:BaseUrl"];
            return Results.Redirect($"{frontendUrl}/auth/callback?token={authResult.Token}");
        });

        // iOS-App: Einmal-Code aus dem Google-Callback gegen JWT + Refresh-Token tauschen
        app.MapPost("/auth/google/exchange", async (
            MobileCodeExchangeRequest request, HttpContext ctx, IAuthService authService, IMemoryCache cache) =>
        {
            if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.CodeVerifier))
                return Results.BadRequest(new { error = "Code und Verifier sind erforderlich." });

            var key = MobileCodeKey(request.Code);
            if (!cache.TryGetValue(key, out MobileLoginGrant? grant) || grant is null)
                return Results.Unauthorized();
            cache.Remove(key);

            if (!PkceMatches(request.CodeVerifier, grant.CodeChallenge))
                return Results.Unauthorized();

            var refreshToken = await authService.CreateRefreshTokenAsync(grant.UserId);
            ctx.Response.Cookies.Append("refresh_token", refreshToken, RefreshCookieOptions(ctx));

            return Results.Ok(new { token = grant.Token, expiresAt = grant.ExpiresAt });
        });

        // JWT per Refresh-Token erneuern (Cookie wird automatisch mitgesendet)
        app.MapPost("/auth/refresh", async (HttpContext ctx, IAuthService authService) =>
        {
            var refreshToken = ctx.Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(refreshToken))
                return Results.Unauthorized();

            var result = await authService.UseRefreshTokenAsync(refreshToken);
            if (result is null)
            {
                ctx.Response.Cookies.Delete("refresh_token", new CookieOptions { Path = $"{ctx.Request.PathBase}/auth" });
                return Results.Unauthorized();
            }

            ctx.Response.Cookies.Append("refresh_token", result.RefreshToken!, RefreshCookieOptions(ctx));

            return Results.Ok(new { token = result.Token, expiresAt = result.ExpiresAt });
        });

        // Abmelden: Refresh-Token widerrufen und Cookie löschen
        app.MapPost("/auth/logout", async (HttpContext ctx, IAuthService authService) =>
        {
            var refreshToken = ctx.Request.Cookies["refresh_token"];
            if (!string.IsNullOrEmpty(refreshToken))
                await authService.RevokeRefreshTokenAsync(refreshToken);

            ctx.Response.Cookies.Delete("refresh_token", new CookieOptions { Path = $"{ctx.Request.PathBase}/auth" });
            return Results.Ok();
        }).RequireAuthorization("JwtApi");

        // Gibt den aktuell eingeloggten Nutzer zurück (JWT-geschützt)
        app.MapGet("/auth/me", async (HttpContext ctx, IUserService userService) =>
        {
            // MapInboundClaims=true mappt sub → NameIdentifier; Fallback für ältere Token
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var id))
                return Results.Unauthorized();

            var user = await userService.GetByIdAsync(id);
            return user is null ? Results.Unauthorized() : Results.Ok(user);
        }).RequireAuthorization("JwtApi");

        // -------------------------------------------------------------------------
        // E-Mail-Registrierung
        // -------------------------------------------------------------------------

        // Neuen Nutzer registrieren – sendet Bestätigungsmail
        app.MapPost("/auth/register", async (
            RegisterRequest request, IAuthService authService, IConfiguration config,
            IPushNotificationService pushService, ApplicationDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email)
                || string.IsNullOrWhiteSpace(request.Password)
                || string.IsNullOrWhiteSpace(request.DisplayName))
                return Results.BadRequest(new { error = "Alle Felder sind erforderlich." });

            if (request.Password.Length < 8)
                return Results.BadRequest(new { error = "Das Passwort muss mindestens 8 Zeichen lang sein." });

            var frontendUrl = config["Frontend:BaseUrl"]!;
            var error = await authService.RegisterAsync(request, frontendUrl);
            if (error is not null)
                return Results.Conflict(new { error });

            var adminIds = await db.UserRoles
                .Where(r => r.Role == Role.Admin)
                .Select(r => r.UserId)
                .ToListAsync();
            _ = pushService.SendToUsersAsync(
                adminIds,
                "Neue Registrierung 👤",
                $"{request.DisplayName} ({request.Email})",
                url: "/admin/users");

            return Results.Ok(new { message = "Registrierung erfolgreich. Bitte bestätige deine E-Mail-Adresse." });
        });

        // E-Mail-Adresse per Token bestätigen
        app.MapPost("/auth/confirm-email", async (ConfirmEmailRequest request, HttpContext ctx, IAuthService authService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return Results.BadRequest(new { error = "Token fehlt." });

            var (result, error) = await authService.ConfirmEmailAsync(request.Token);
            if (result is null)
                return Results.BadRequest(new { error });

            var refreshToken = await authService.CreateRefreshTokenAsync(result.User.Id);
            ctx.Response.Cookies.Append("refresh_token", refreshToken, RefreshCookieOptions(ctx));

            return Results.Ok(new { token = result.Token, expiresAt = result.ExpiresAt });
        });

        // Bestätigungsmail erneut senden
        app.MapPost("/auth/resend-confirmation", async (ResendConfirmationRequest request, IAuthService authService, IConfiguration config) =>
        {
            if (!string.IsNullOrWhiteSpace(request.Email))
                await authService.ResendConfirmationAsync(request.Email, config["Frontend:BaseUrl"]!);
            return Results.Ok(); // Immer 200 – keine Information preisgeben
        });

        // Login mit E-Mail + Passwort
        app.MapPost("/auth/login", async (LoginRequest request, HttpContext ctx, IAuthService authService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { error = "E-Mail und Passwort sind erforderlich." });

            var (result, error) = await authService.LoginAsync(request);
            if (result is null)
            {
                return error == "email_not_confirmed"
                    ? Results.Json(new { error }, statusCode: 403)
                    : Results.Unauthorized();
            }

            var refreshToken = await authService.CreateRefreshTokenAsync(result.User.Id);
            ctx.Response.Cookies.Append("refresh_token", refreshToken, RefreshCookieOptions(ctx));

            return Results.Ok(new { token = result.Token, expiresAt = result.ExpiresAt });
        });

        // -------------------------------------------------------------------------
        // Passwort zurücksetzen
        // -------------------------------------------------------------------------

        // Passwort-Reset anfordern – verrät nie ob die E-Mail existiert
        app.MapPost("/auth/request-password-reset", async (PasswordResetRequest request, IAuthService authService, IConfiguration config) =>
        {
            if (!string.IsNullOrWhiteSpace(request.Email))
                await authService.RequestPasswordResetAsync(request.Email, config["Frontend:BaseUrl"]!);
            return Results.Ok();
        });

        // Neues Passwort setzen
        app.MapPost("/auth/reset-password", async (ResetPasswordRequest request, IAuthService authService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
                return Results.BadRequest(new { error = "Token und neues Passwort sind erforderlich." });

            if (request.NewPassword.Length < 8)
                return Results.BadRequest(new { error = "Das Passwort muss mindestens 8 Zeichen lang sein." });

            var ok = await authService.ResetPasswordAsync(request);
            return ok
                ? Results.Ok(new { message = "Passwort wurde erfolgreich zurückgesetzt." })
                : Results.BadRequest(new { error = "Der Link ist ungültig oder abgelaufen." });
        });

        return app;
    }

    private const string MobileClient = "ios";
    private const string MobileCallbackUrl = "akakraft://auth";
    private const string ClientItem = "akakraft.client";
    private const string ChallengeItem = "akakraft.code_challenge";

    private sealed record MobileLoginGrant(Guid UserId, string CodeChallenge, string Token, DateTime ExpiresAt);

    private static string MobileCodeKey(string code) => $"mobile-login:{code}";

    // RFC 7636: S256-Challenge ist Base64url(SHA256(verifier)) ohne Padding = 43 Zeichen.
    private static bool IsValidPkceChallenge(string? challenge) =>
        challenge is { Length: 43 } && challenge.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    private static bool PkceMatches(string verifier, string challenge)
    {
        var computed = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(computed), Encoding.ASCII.GetBytes(challenge));
    }

    // Lokal (HTTP): Secure=false + SameSite=Lax – Browser speichert den Cookie,
    // localhost-Ports gelten als same-site, sodass der Cookie mitgesendet wird.
    // Produktion (HTTPS): Secure=true + SameSite=None – nötig für cross-origin.
    private static CookieOptions RefreshCookieOptions(HttpContext ctx) => new()
    {
        HttpOnly = true,
        Secure   = ctx.Request.IsHttps,
        SameSite = ctx.Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
        MaxAge   = TimeSpan.FromDays(30),
        Path     = $"{ctx.Request.PathBase}/auth",
    };
}