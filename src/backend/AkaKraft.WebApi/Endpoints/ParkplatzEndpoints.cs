using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Entities;
using AkaKraft.Domain.Enums;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.WebApi.Endpoints;

internal static class ParkplatzEndpoints
{
    internal static WebApplication MapParkplatzEndpoints(this WebApplication app)
    {
        // Übersicht: Konten-Status + eigene Berechtigung + anstehende Reservierungen
        app.MapGet("/parkplatz/overview", async (HttpContext ctx, IParkplatzService svc) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId)) return Results.Unauthorized();
            return Results.Ok(await svc.GetOverviewAsync(userId));
        }).RequireAuthorization("AnyRole");

        // Historie aller Belegungen (neueste zuerst)
        app.MapGet("/parkplatz/historie", async (HttpContext ctx, ApplicationDbContext db, int? limit) =>
        {
            if (!ctx.TryGetCurrentUserId(out _)) return Results.Unauthorized();

            var now = DateTime.UtcNow;
            var take = Math.Clamp(limit ?? 50, 1, 200);

            var claims = await db.ParkClaims
                .Include(c => c.ParkAccount)
                .Include(c => c.User)
                .OrderByDescending(c => c.EinfahrtAt)
                .Take(take)
                .ToListAsync();

            var displayNames = await db.UserPreferences
                .Where(p => claims.Select(c => c.UserId).Contains(p.UserId) && p.DisplayName != null)
                .ToDictionaryAsync(p => p.UserId, p => p.DisplayName!);

            var eintraege = claims.Select(c => new ParkHistorieEintragDto(
                c.Id,
                c.ParkAccount.Label,
                displayNames.GetValueOrDefault(c.UserId) ?? c.User.Name,
                c.Kennzeichen,
                c.FahrzeugBezeichnung,
                c.EinfahrtAt,
                c.FreigegebenAt,
                c.AutoExpiresAt,
                c.BerechtigungArt.ToString(),
                c.BestaetigungHinweis,
                c.BookingEventId,
                Status: c.FreigegebenAt != null ? "Freigegeben"
                      : c.AutoExpiresAt <= now ? "Abgelaufen"
                      : "Aktiv"));

            return Results.Ok(eintraege);
        }).RequireAuthorization("AnyRole");

        // Parkkonto übernehmen (Check-in)
        app.MapPost("/parkplatz/checkin", async (
            ParkCheckinRequest req, HttpContext ctx,
            ApplicationDbContext db, IParkplatzService svc) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId)) return Results.Unauthorized();

            var now = DateTime.UtcNow;

            var account = await db.ParkAccounts.FirstOrDefaultAsync(a => a.Id == req.ParkAccountId);
            if (account is null) return Results.NotFound("Parkkonto nicht gefunden.");

            // Konto muss frei sein
            var kontoBelegt = await db.ParkClaims.AnyAsync(c =>
                c.ParkAccountId == account.Id && c.FreigegebenAt == null && c.AutoExpiresAt > now);
            if (kontoBelegt)
                return Results.BadRequest("Dieses Parkkonto ist gerade belegt.");

            // Nutzer darf nicht schon ein anderes Konto halten
            var haeltBereits = await db.ParkClaims.AnyAsync(c =>
                c.UserId == userId && c.FreigegebenAt == null && c.AutoExpiresAt > now);
            if (haeltBereits)
                return Results.BadRequest("Du hast bereits ein Parkkonto belegt. Bitte zuerst freigeben.");

            // Fahrzeug / Kennzeichen ermitteln
            string kennzeichen;
            string bezeichnung;
            if (req.FahrzeugId is { } fahrzeugId)
            {
                var fahrzeug = await db.Fahrzeuge.FirstOrDefaultAsync(f => f.Id == fahrzeugId && f.UserId == userId);
                if (fahrzeug is null) return Results.BadRequest("Fahrzeug nicht gefunden.");
                kennzeichen = fahrzeug.Kennzeichen;
                bezeichnung = $"{fahrzeug.Marke} {fahrzeug.Modell}".Trim();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(req.Kennzeichen))
                    return Results.BadRequest("Bitte ein Fahrzeug wählen oder ein Kennzeichen angeben.");
                kennzeichen = FahrzeugEndpoints.NormalizeKennzeichen(req.Kennzeichen);
                bezeichnung = (req.FahrzeugBezeichnung ?? string.Empty).Trim();
            }

            if (kennzeichen.Length is 0 or > 16)
                return Results.BadRequest("Ungültiges Kennzeichen.");

            // Konflikt: gleiches Kennzeichen bereits auf einem Konto aktiv
            var kennzeichenAktiv = await db.ParkClaims.AnyAsync(c =>
                c.FreigegebenAt == null && c.AutoExpiresAt > now && c.Kennzeichen == kennzeichen);
            if (kennzeichenAktiv)
                return Results.BadRequest("Dieses Kennzeichen ist bereits auf einem Parkkonto aktiv.");

            // Berechtigung serverseitig prüfen
            var berechtigung = await svc.GetBerechtigungAsync(userId, now);
            if (berechtigung.ErfordertBestaetigung && !req.BestaetigungAkzeptiert)
                return Results.BadRequest("Bitte bestätige die Nutzungsberechtigung.");

            var einfahrt = (req.EinfahrtAt?.ToUniversalTime()) ?? now;
            if (einfahrt > now) einfahrt = now;
            if (einfahrt < now.AddHours(-12)) einfahrt = now.AddHours(-12);

            var voraussichtlichBis = req.VoraussichtlichBis?.ToUniversalTime();
            if (voraussichtlichBis is { } vb && (vb <= einfahrt || vb > einfahrt.AddHours(24)))
                voraussichtlichBis = null;

            var art = Enum.TryParse<ParkBerechtigungArt>(berechtigung.Art, out var parsed)
                ? parsed : ParkBerechtigungArt.Spontan;

            var claim = new ParkClaim
            {
                Id = Guid.NewGuid(),
                ParkAccountId = account.Id,
                UserId = userId,
                Kennzeichen = kennzeichen,
                FahrzeugBezeichnung = bezeichnung,
                EinfahrtAt = einfahrt,
                VoraussichtlichBis = voraussichtlichBis,
                AutoExpiresAt = einfahrt.AddHours(24),
                BerechtigungArt = art,
                BestaetigungHinweis = art == ParkBerechtigungArt.Automatisch ? null : berechtigung.Hinweis,
                BookingEventId = string.IsNullOrWhiteSpace(req.BookingEventId) ? null : req.BookingEventId,
            };

            db.ParkClaims.Add(claim);
            await db.SaveChangesAsync();

            await db.Entry(claim).Reference(c => c.User).LoadAsync();
            var displayName = await db.UserPreferences
                .Where(p => p.UserId == userId && p.DisplayName != null)
                .Select(p => p.DisplayName!)
                .FirstOrDefaultAsync() ?? claim.User.Name;

            return Results.Ok(new ParkClaimDto(
                claim.Id, claim.ParkAccountId, claim.UserId, displayName, claim.User.PictureUrl,
                claim.Kennzeichen, claim.FahrzeugBezeichnung, claim.EinfahrtAt,
                claim.VoraussichtlichBis, claim.AutoExpiresAt, claim.BerechtigungArt.ToString()));
        }).RequireAuthorization("AnyRole");

        // Voraussichtliche Standdauer aktualisieren
        app.MapPut("/parkplatz/claims/{id:guid}", async (
            Guid id, ParkClaimUpdateRequest req, HttpContext ctx, ApplicationDbContext db) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId)) return Results.Unauthorized();

            var claim = await db.ParkClaims.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);
            if (claim is null) return Results.NotFound();
            if (claim.UserId != userId && !ctx.IsPrivileged()) return Results.Forbid();

            var vb = req.VoraussichtlichBis?.ToUniversalTime();
            if (vb is { } v && (v <= claim.EinfahrtAt || v > claim.AutoExpiresAt))
                vb = null;
            claim.VoraussichtlichBis = vb;
            await db.SaveChangesAsync();

            return Results.Ok(new ParkClaimDto(
                claim.Id, claim.ParkAccountId, claim.UserId, claim.User.Name, claim.User.PictureUrl,
                claim.Kennzeichen, claim.FahrzeugBezeichnung, claim.EinfahrtAt,
                claim.VoraussichtlichBis, claim.AutoExpiresAt, claim.BerechtigungArt.ToString()));
        }).RequireAuthorization("AnyRole");

        // Parkkonto freigeben (Ausfahrt)
        app.MapPost("/parkplatz/claims/{id:guid}/freigeben", async (
            Guid id, HttpContext ctx, ApplicationDbContext db) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId)) return Results.Unauthorized();

            var claim = await db.ParkClaims.FirstOrDefaultAsync(c => c.Id == id);
            if (claim is null) return Results.NotFound();
            if (claim.UserId != userId && !ctx.IsPrivileged()) return Results.Forbid();
            if (claim.FreigegebenAt != null) return Results.NoContent();

            claim.FreigegebenAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("AnyRole");

        // Problem melden ("Konto zeigt frei, aber es steht noch ein Auto" o. ä.)
        app.MapPost("/parkplatz/accounts/{id:guid}/problem", async (
            Guid id, HttpContext ctx, ApplicationDbContext db, IPushNotificationService push) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId)) return Results.Unauthorized();

            var account = await db.ParkAccounts.FirstOrDefaultAsync(a => a.Id == id);
            if (account is null) return Results.NotFound();

            var melderName = await db.UserPreferences
                .Where(p => p.UserId == userId && p.DisplayName != null)
                .Select(p => p.DisplayName!)
                .FirstOrDefaultAsync()
                ?? (await db.Users.FindAsync(userId))?.Name
                ?? "Ein Mitglied";

            var empfaenger = await db.UserRoles
                .Where(ur => ur.Role == Role.Hallenwart || ur.Role == Role.Admin
                          || ur.Role == Role.Chairman || ur.Role == Role.ViceChairman)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            if (empfaenger.Count > 0)
            {
                _ = push.SendToUsersAsync(empfaenger,
                    $"Parkkonto-Problem: {account.Label}",
                    $"{melderName} meldet ein Problem mit {account.Label}. Bitte prüfen.",
                    url: "/parkplatz");
            }

            return Results.NoContent();
        }).RequireAuthorization("AnyRole");

        // Konto-Stammdaten pflegen (Label, Portal-Link, Notiz)
        app.MapPut("/parkplatz/accounts/{id:guid}", async (
            Guid id, ParkAccountUpdateRequest req, ApplicationDbContext db) =>
        {
            var account = await db.ParkAccounts.FirstOrDefaultAsync(a => a.Id == id);
            if (account is null) return Results.NotFound();

            if (string.IsNullOrWhiteSpace(req.Label)) return Results.BadRequest("Label ist erforderlich.");

            account.Label = req.Label.Trim();
            account.PortalUrl = string.IsNullOrWhiteSpace(req.PortalUrl) ? null : req.PortalUrl.Trim();
            account.Notiz = string.IsNullOrWhiteSpace(req.Notiz) ? null : req.Notiz.Trim();
            await db.SaveChangesAsync();

            return Results.Ok(new ParkAccountStatusDto(
                account.Id, account.Label, account.PortalUrl, account.Notiz, IstFrei: true, Belegung: null));
        }).RequireAuthorization("VorstandOrAdmin");

        return app;
    }
}
