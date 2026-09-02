using AkaKraft.Application.DTOs;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.WebApi.Endpoints;

internal static class FahrzeugEndpoints
{
    internal static WebApplication MapFahrzeugEndpoints(this WebApplication app)
    {
        // Eigene Fahrzeuge auflisten
        app.MapGet("/fahrzeuge", async (HttpContext ctx, ApplicationDbContext db) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId)) return Results.Unauthorized();

            var list = await db.Fahrzeuge
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.IstStandard)
                .ThenBy(f => f.Marke).ThenBy(f => f.Modell)
                .Select(f => new FahrzeugDto(f.Id, f.Marke, f.Modell, f.Kennzeichen, f.IstStandard))
                .ToListAsync();

            return Results.Ok(list);
        }).RequireAuthorization("AnyRole");

        // Fahrzeug anlegen
        app.MapPost("/fahrzeuge", async (SaveFahrzeugRequest req, HttpContext ctx, ApplicationDbContext db) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId)) return Results.Unauthorized();

            var validation = Validate(req);
            if (validation is not null) return Results.BadRequest(validation);

            var fahrzeug = new Fahrzeug
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Marke = req.Marke.Trim(),
                Modell = string.IsNullOrWhiteSpace(req.Modell) ? null : req.Modell.Trim(),
                Kennzeichen = NormalizeKennzeichen(req.Kennzeichen),
                IstStandard = req.IstStandard,
            };

            if (fahrzeug.IstStandard)
                await ClearStandardAsync(db, userId);

            db.Fahrzeuge.Add(fahrzeug);
            await db.SaveChangesAsync();

            return Results.Created($"/fahrzeuge/{fahrzeug.Id}",
                new FahrzeugDto(fahrzeug.Id, fahrzeug.Marke, fahrzeug.Modell, fahrzeug.Kennzeichen, fahrzeug.IstStandard));
        }).RequireAuthorization("AnyRole");

        // Fahrzeug bearbeiten
        app.MapPut("/fahrzeuge/{id:guid}", async (Guid id, SaveFahrzeugRequest req, HttpContext ctx, ApplicationDbContext db) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId)) return Results.Unauthorized();

            var fahrzeug = await db.Fahrzeuge.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
            if (fahrzeug is null) return Results.NotFound();

            var validation = Validate(req);
            if (validation is not null) return Results.BadRequest(validation);

            fahrzeug.Marke = req.Marke.Trim();
            fahrzeug.Modell = string.IsNullOrWhiteSpace(req.Modell) ? null : req.Modell.Trim();
            fahrzeug.Kennzeichen = NormalizeKennzeichen(req.Kennzeichen);

            if (req.IstStandard && !fahrzeug.IstStandard)
                await ClearStandardAsync(db, userId);
            fahrzeug.IstStandard = req.IstStandard;

            await db.SaveChangesAsync();

            return Results.Ok(new FahrzeugDto(fahrzeug.Id, fahrzeug.Marke, fahrzeug.Modell, fahrzeug.Kennzeichen, fahrzeug.IstStandard));
        }).RequireAuthorization("AnyRole");

        // Fahrzeug löschen
        app.MapDelete("/fahrzeuge/{id:guid}", async (Guid id, HttpContext ctx, ApplicationDbContext db) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId)) return Results.Unauthorized();

            var fahrzeug = await db.Fahrzeuge.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
            if (fahrzeug is null) return Results.NotFound();

            db.Fahrzeuge.Remove(fahrzeug);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("AnyRole");

        return app;
    }

    private static string? Validate(SaveFahrzeugRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Marke)) return "Marke ist erforderlich.";
        if (string.IsNullOrWhiteSpace(req.Kennzeichen)) return "Kennzeichen ist erforderlich.";
        if (req.Marke.Trim().Length > 80) return "Marke ist zu lang.";
        if (NormalizeKennzeichen(req.Kennzeichen).Length > 16) return "Kennzeichen ist zu lang.";
        return null;
    }

    private static async Task ClearStandardAsync(ApplicationDbContext db, Guid userId)
    {
        var bisher = await db.Fahrzeuge.Where(f => f.UserId == userId && f.IstStandard).ToListAsync();
        foreach (var f in bisher) f.IstStandard = false;
    }

    internal static string NormalizeKennzeichen(string raw) =>
        raw.Trim().ToUpperInvariant().Replace("  ", " ");
}
