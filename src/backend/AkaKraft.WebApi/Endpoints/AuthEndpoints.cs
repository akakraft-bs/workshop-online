using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

namespace AkaKraft.WebApi.Endpoints;

internal static class AuthEndpoints
{
    // -------------------------------------------------------------------------
    // Auth Endpoints
    // -------------------------------------------------------------------------
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        // Schritt 1: Frontend leitet hierher weiter → Google-Login startet
        app.MapGet("/auth/login/google", (HttpContext ctx) =>
            Results.Challenge(
                new AuthenticationProperties { RedirectUri = $"{ctx.Request.PathBase}/auth/callback" },
                [GoogleDefaults.AuthenticationScheme]));

        // Schritt 2: Google-Callback → Nutzer anlegen/laden → JWT + Refresh-Token erzeugen → Frontend weiterleiten
        app.MapGet("/auth/callback", async (HttpContext ctx, IAuthService authService, IConfiguration config) =>
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
            var refreshToken = await authService.CreateRefreshTokenAsync(authResult.User.Id);

            ctx.Response.Cookies.Append("refresh_token", refreshToken, RefreshCookieOptions(ctx));

            var frontendUrl = config["Frontend:BaseUrl"];
            return Results.Redirect($"{frontendUrl}/auth/callback?token={authResult.Token}");
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
        app.MapPost("/auth/register", async (RegisterRequest request, IAuthService authService, IConfiguration config) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email)
                || string.IsNullOrWhiteSpace(request.Password)
                || string.IsNullOrWhiteSpace(request.DisplayName))
                return Results.BadRequest(new { error = "Alle Felder sind erforderlich." });

            if (request.Password.Length < 8)
                return Results.BadRequest(new { error = "Das Passwort muss mindestens 8 Zeichen lang sein." });

            var frontendUrl = config["Frontend:BaseUrl"]!;
            var error = await authService.RegisterAsync(request, frontendUrl);
            return error is null
                ? Results.Ok(new { message = "Registrierung erfolgreich. Bitte bestätige deine E-Mail-Adresse." })
                : Results.Conflict(new { error });
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