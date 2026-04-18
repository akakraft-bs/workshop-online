using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AkaKraft.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

namespace AkaKraft.WebApi.Endpoints;

public static class AuthApi
{
    // -------------------------------------------------------------------------
    // Auth Endpoints
    // -------------------------------------------------------------------------
    public static WebApplication AddAuthApi(this WebApplication app)
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

            ctx.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                MaxAge = TimeSpan.FromDays(30),
                Path = "/api/auth",
            });

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
                ctx.Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/api/auth" });
                return Results.Unauthorized();
            }

            ctx.Response.Cookies.Append("refresh_token", result.RefreshToken!, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                MaxAge = TimeSpan.FromDays(30),
                Path = "/api/auth",
            });

            return Results.Ok(new { token = result.Token, expiresAt = result.ExpiresAt });
        });

        // Abmelden: Refresh-Token widerrufen und Cookie löschen
        app.MapPost("/auth/logout", async (HttpContext ctx, IAuthService authService) =>
        {
            var refreshToken = ctx.Request.Cookies["refresh_token"];
            if (!string.IsNullOrEmpty(refreshToken))
                await authService.RevokeRefreshTokenAsync(refreshToken);

            ctx.Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/api/auth" });
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

        return app;
    }
}