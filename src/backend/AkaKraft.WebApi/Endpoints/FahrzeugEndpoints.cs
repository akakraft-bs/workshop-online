using AkaKraft.Application.DTOs;
using AkaKraft.Domain.Entities;
using AkaKraft.Domain.Enums;
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
                .Select(f => new FahrzeugDto(f.Id, f.Marke, f.Modell, f.Kennzeichen, f.IstStandard, f.Art))
                .ToListAsync();

            return Results.Ok(list);
        }).RequireAuthorization("AnyRole");

        // Fahrzeug anlegen
        app.MapPost("/fahrzeuge", async (SaveFahrzeugRequest req, HttpContext ctx, ApplicationDbContext db) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId)) return Results.Unauthorized();

            var art = req.Art ?? FahrzeugArt.Pkw;
            var validation = Validate(req, art);
            if (validation is not null) return Results.BadRequest(validation);

            var fahrzeug = new Fahrzeug
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Marke = req.Marke.Trim(),
                Modell = string.IsNullOrWhiteSpace(req.Modell) ? null : req.Modell.Trim(),
                Kennzeichen = NormalizeKennzeichen(req.Kennzeichen ?? string.Empty),
                IstStandard = req.IstStandard,
                Art = art,
            };

            if (fahrzeug.IstStandard)
                await ClearStandardAsync(db, userId);

            db.Fahrzeuge.Add(fahrzeug);
            await db.SaveChangesAsync();

            return Results.Created($"/fahrzeuge/{fahrzeug.Id}",
                ToDto(fahrzeug));
        }).RequireAuthorization("AnyRole");

        // Fahrzeug bearbeiten
        app.MapPut("/fahrzeuge/{id:guid}", async (Guid id, SaveFahrzeugRequest req, HttpContext ctx, ApplicationDbContext db) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId)) return Results.Unauthorized();

            var fahrzeug = await db.Fahrzeuge.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
            if (fahrzeug is null) return Results.NotFound();

            var art = req.Art ?? fahrzeug.Art;
            var validation = Validate(req, art);
            if (validation is not null) return Results.BadRequest(validation);

            fahrzeug.Marke = req.Marke.Trim();
            fahrzeug.Modell = string.IsNullOrWhiteSpace(req.Modell) ? null : req.Modell.Trim();
            fahrzeug.Kennzeichen = NormalizeKennzeichen(req.Kennzeichen ?? string.Empty);
            fahrzeug.Art = art;

            if (req.IstStandard && !fahrzeug.IstStandard)
                await ClearStandardAsync(db, userId);
            fahrzeug.IstStandard = req.IstStandard;

            await db.SaveChangesAsync();

            return Results.Ok(ToDto(fahrzeug));
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

    private static string? Validate(SaveFahrzeugRequest req, FahrzeugArt art)
    {
        if (!Enum.IsDefined(art)) return "Ungültige Fahrzeugart.";
        if (string.IsNullOrWhiteSpace(req.Marke)) return "Marke ist erforderlich.";
        if (KennzeichenPflicht(art) && string.IsNullOrWhiteSpace(req.Kennzeichen)) return "Kennzeichen ist erforderlich.";
        if (req.Marke.Trim().Length > 80) return "Marke ist zu lang.";
        if (NormalizeKennzeichen(req.Kennzeichen ?? string.Empty).Length > 16) return "Kennzeichen ist zu lang.";
        return null;
    }

    /// <summary>Fahrräder brauchen kein Kennzeichen, alle anderen Arten schon.</summary>
    internal static bool KennzeichenPflicht(FahrzeugArt art) => art != FahrzeugArt.Fahrrad;

    private static FahrzeugDto ToDto(Fahrzeug f) =>
        new(f.Id, f.Marke, f.Modell, f.Kennzeichen, f.IstStandard, f.Art);

    private static async Task ClearStandardAsync(ApplicationDbContext db, Guid userId)
    {
        var bisher = await db.Fahrzeuge.Where(f => f.UserId == userId && f.IstStandard).ToListAsync();
        foreach (var f in bisher) f.IstStandard = false;
    }

    internal static string NormalizeKennzeichen(string raw) =>
        raw.Trim().ToUpperInvariant().Replace("  ", " ");
}
