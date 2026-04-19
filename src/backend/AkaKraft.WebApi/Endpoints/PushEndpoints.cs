using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.WebApi.Endpoints;

internal static class PushEndpoints
{
    internal static void MapPushEndpoints(this WebApplication app)
    {
        app.MapPost("/push/tokens", async (
            HttpContext ctx, RegisterFcmTokenDto dto, ApplicationDbContext db) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Token))
                return Results.BadRequest("Token darf nicht leer sein.");

            var existing = await db.FcmTokens.FirstOrDefaultAsync(t => t.Token == dto.Token);
            if (existing is null)
            {
                db.FcmTokens.Add(new AkaKraft.Domain.Entities.FcmToken { UserId = userId, Token = dto.Token });
            }
            else
            {
                existing.UserId = userId;
                existing.RegisteredAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
            return Results.Ok();
        }).RequireAuthorization("JwtApi");

        app.MapDelete("/push/tokens/{token}", async (
            string token, HttpContext ctx, ApplicationDbContext db) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId))
                return Results.Unauthorized();

            var existing = await db.FcmTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.UserId == userId);

            if (existing is not null)
            {
                db.FcmTokens.Remove(existing);
                await db.SaveChangesAsync();
            }

            return Results.Ok();
        }).RequireAuthorization("JwtApi");

        app.MapPost("/admin/push/test", async (
            SendTestPushDto dto, IPushNotificationService pushService) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Body))
                return Results.BadRequest("Titel und Text dürfen nicht leer sein.");

            if (dto.UserId.HasValue)
                await pushService.SendToUserAsync(dto.UserId.Value, dto.Title, dto.Body);
            else
                await pushService.SendToUsersWithPreferenceAsync(_ => true, dto.Title, dto.Body);

            return Results.Ok();
        }).RequireAuthorization("AdminOnly");
    }
}
