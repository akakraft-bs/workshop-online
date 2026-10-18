using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.WebApi.Endpoints;

internal static class HallenCheckEndpoints
{
    public static WebApplication MapHallenCheckEndpoints(this WebApplication app)
    {
        // Wer ist gerade an der Halle?
        app.MapGet("/halle/anwesend", async (ApplicationDbContext db) =>
        {
            var now = DateTime.UtcNow;
            var list = await db.HallenChecks
                .Include(h => h.User)
                .Where(h => h.ExpiresAt > now)
                .OrderBy(h => h.CheckedInAt)
                .Select(h => new HallenCheckDto(
                    h.Id,
                    h.UserId,
                    db.UserPreferences
                        .Where(p => p.UserId == h.UserId && p.DisplayName != null)
                        .Select(p => p.DisplayName!)
                        .FirstOrDefault() ?? h.User.Name,
                    h.User.PictureUrl,
                    h.Message,
                    h.CheckedInAt,
                    h.ExpiresAt))
                .ToListAsync();
            return Results.Ok(list);
        }).RequireAuthorization("AnyRole");

        // Einchecken (Upsert – vorhandener Check-in wird ersetzt)
        app.MapPost("/halle/checkin", async (
            HttpContext ctx, HallenCheckInRequest req,
            ApplicationDbContext db, IPushNotificationService push) =>
        {
            var userId = GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();

            var now = DateTime.UtcNow;

            // Ältere Check-ins desselben Nutzers entfernen
            var existing = await db.HallenChecks
                .Where(h => h.UserId == userId.Value)
                .ToListAsync();
            db.HallenChecks.RemoveRange(existing);

            var check = new HallenCheck
            {
                Id          = Guid.NewGuid(),
                UserId      = userId.Value,
                Message     = string.IsNullOrWhiteSpace(req.Message) ? null : req.Message.Trim(),
                CheckedInAt = now,
                ExpiresAt   = now.AddHours(4),
            };
            db.HallenChecks.Add(check);
            await db.SaveChangesAsync();

            // Display-Name für Push-Benachrichtigung ermitteln
            var displayName = await db.UserPreferences
                .Where(p => p.UserId == userId.Value && p.DisplayName != null)
                .Select(p => p.DisplayName!)
                .FirstOrDefaultAsync()
                ?? (await db.Users.FindAsync(userId.Value))?.Name
                ?? "Jemand";

            var pushBody = string.IsNullOrWhiteSpace(check.Message)
                ? $"{displayName} ist jetzt an der Halle!"
                : $"{displayName}: {check.Message}";

            var otherUserIds = await db.Users
                .Where(u => u.Id != userId.Value)
                .Select(u => u.Id)
                .ToListAsync();
            _ = push.SendToUsersAsync(otherUserIds, "An der Halle", pushBody, url: "/app/dashboard");

            await db.Entry(check).Reference(h => h.User).LoadAsync();
            var dto = new HallenCheckDto(
                check.Id, check.UserId, displayName,
                check.User.PictureUrl, check.Message,
                check.CheckedInAt, check.ExpiresAt);
            return Results.Ok(dto);
        }).RequireAuthorization("AnyRole");

        // Auschecken
        app.MapDelete("/halle/checkin", async (HttpContext ctx, ApplicationDbContext db) =>
        {
            var userId = GetUserId(ctx);
            if (userId is null) return Results.Unauthorized();

            var checks = await db.HallenChecks
                .Where(h => h.UserId == userId.Value)
                .ToListAsync();
            db.HallenChecks.RemoveRange(checks);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("AnyRole");

        // Standort der Halle für den Standort-Check-in der Mobile-Apps
        app.MapGet("/halle/standort", async (ApplicationDbContext db) =>
        {
            var standort = await db.HallenStandorte.FirstOrDefaultAsync();
            return standort is null ? Results.NoContent() : Results.Ok(ToDto(standort));
        }).RequireAuthorization("AnyRole");

        app.MapPut("/halle/standort", async (SetHallenStandortRequest req, ApplicationDbContext db) =>
        {
            if (req.Latitude is < -90 or > 90 || req.Longitude is < -180 or > 180
                || double.IsNaN(req.Latitude) || double.IsNaN(req.Longitude))
                return Results.BadRequest("Ungültige Koordinaten.");

            var standort = await db.HallenStandorte.FirstOrDefaultAsync();
            if (standort is null)
            {
                standort = new HallenStandort { Id = Guid.NewGuid() };
                db.HallenStandorte.Add(standort);
            }
            standort.Latitude = req.Latitude;
            standort.Longitude = req.Longitude;
            await db.SaveChangesAsync();
            return Results.Ok(ToDto(standort));
        }).RequireAuthorization("AdminOnly");

        app.MapDelete("/halle/standort", async (ApplicationDbContext db) =>
        {
            var standort = await db.HallenStandorte.FirstOrDefaultAsync();
            if (standort is not null)
            {
                db.HallenStandorte.Remove(standort);
                await db.SaveChangesAsync();
            }
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        return app;
    }

    /// <summary>Umkreis um die Halle, in dem die Apps zum Einchecken fragen bzw. beim Verlassen auschecken.</summary>
    private const int GeofenceRadiusMeter = 200;

    private static HallenStandortDto ToDto(HallenStandort s) =>
        new(s.Latitude, s.Longitude, GeofenceRadiusMeter, s.UpdatedAt ?? s.CreatedAt);

    private static Guid? GetUserId(HttpContext ctx)
    {
        var raw = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
