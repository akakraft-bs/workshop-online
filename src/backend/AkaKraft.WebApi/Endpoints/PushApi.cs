using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AkaKraft.Application.DTOs;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.WebApi.Endpoints;

public static class PushApi
{
    public static WebApplication AddPushApi(this WebApplication app)
    {
        // -------------------------------------------------------------------------
        // Push-Notification Token Endpoints
        // -------------------------------------------------------------------------

        // FCM-Token für dieses Gerät registrieren
        _ = app.MapPost("/push/tokens", async (
            HttpContext ctx,
            RegisterFcmTokenDto dto,
            ApplicationDbContext db) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Token))
                return Results.BadRequest("Token darf nicht leer sein.");

            // Upsert: Token existiert bereits → RegisteredAt aktualisieren
            var existing = await db.FcmTokens
                .FirstOrDefaultAsync(t => t.Token == dto.Token);

            if (existing is null)
            {
                db.FcmTokens.Add(new AkaKraft.Domain.Entities.FcmToken
                {
                    UserId = parsedUserId,
                    Token = dto.Token,
                });
            }
            else
            {
                existing.UserId = parsedUserId;
                existing.RegisteredAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
            return Results.Ok();
        }).RequireAuthorization("JwtApi");

        // FCM-Token für dieses Gerät entfernen
        app.MapDelete("/push/tokens/{token}", async (
            string token,
            HttpContext ctx,
            ApplicationDbContext db) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var existing = await db.FcmTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.UserId == parsedUserId);

            if (existing is not null)
            {
                db.FcmTokens.Remove(existing);
                await db.SaveChangesAsync();
            }

            return Results.Ok();
        }).RequireAuthorization("JwtApi");
        return app;
    }
}