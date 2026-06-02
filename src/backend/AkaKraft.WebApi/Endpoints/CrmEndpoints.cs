using AkaKraft.Application.DTOs;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.WebApi.Endpoints;

internal static class CrmEndpoints
{
    internal static WebApplication MapCrmEndpoints(this WebApplication app)
    {
        // -------------------------------------------------------------------------
        // Partner
        // -------------------------------------------------------------------------

        app.MapGet("/crm/partner", async (ApplicationDbContext db) =>
        {
            var partners = await db.Partner
                .Include(p => p.Kontakteintraege)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return Results.Ok(partners.Select(p => new PartnerOverviewDto(
                p.Id, p.Name, p.Kategorie, p.Status, p.Website,
                p.Kontakteintraege.Count,
                p.Kontakteintraege.Count > 0
                    ? p.Kontakteintraege.Max(k => k.Datum)
                    : null
            )));
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapGet("/crm/partner/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var p = await db.Partner
                .Include(p => p.Ansprechpartner)
                .Include(p => p.Kontakteintraege)
                    .ThenInclude(k => k.Ansprechpartner)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (p is null) return Results.NotFound();

            return Results.Ok(new PartnerDetailDto(
                p.Id, p.Name, p.Kategorie, p.Status, p.Website, p.Notizen,
                p.Ansprechpartner.Select(a => new AnsprechpartnerDto(
                    a.Id, a.Name, a.Position, a.Email, a.Telefon, a.Notizen)),
                p.Kontakteintraege
                    .OrderByDescending(k => k.Datum)
                    .Select(k => new KontakteintragDto(
                        k.Id, k.AnsprechpartnerId, k.Ansprechpartner?.Name,
                        k.Datum, k.Kanal, k.Reaktion, k.Zusammenfassung, k.NaechsteSchritte,
                        k.CreatedAt))
            ));
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapPost("/crm/partner", async (CreatePartnerDto dto, ApplicationDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Results.BadRequest("Name darf nicht leer sein.");

            var partner = new Partner
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Kategorie = dto.Kategorie?.Trim(),
                Status = dto.Status,
                Website = dto.Website?.Trim(),
                Notizen = dto.Notizen?.Trim(),
            };
            db.Partner.Add(partner);
            await db.SaveChangesAsync();

            return Results.Created($"/crm/partner/{partner.Id}",
                new PartnerOverviewDto(partner.Id, partner.Name, partner.Kategorie,
                    partner.Status, partner.Website, 0, null));
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapPut("/crm/partner/{id:guid}", async (Guid id, UpdatePartnerDto dto, ApplicationDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Results.BadRequest("Name darf nicht leer sein.");

            var partner = await db.Partner.FindAsync(id);
            if (partner is null) return Results.NotFound();

            partner.Name = dto.Name.Trim();
            partner.Kategorie = dto.Kategorie?.Trim();
            partner.Status = dto.Status;
            partner.Website = dto.Website?.Trim();
            partner.Notizen = dto.Notizen?.Trim();
            await db.SaveChangesAsync();

            return Results.Ok(new PartnerOverviewDto(partner.Id, partner.Name, partner.Kategorie,
                partner.Status, partner.Website, 0, null));
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapDelete("/crm/partner/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var partner = await db.Partner.FindAsync(id);
            if (partner is null) return Results.NotFound();
            db.Partner.Remove(partner);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("VorstandOrAdmin");

        // -------------------------------------------------------------------------
        // Ansprechpartner
        // -------------------------------------------------------------------------

        app.MapPost("/crm/partner/{partnerId:guid}/ansprechpartner",
            async (Guid partnerId, CreateAnsprechpartnerDto dto, ApplicationDbContext db) =>
        {
            if (!await db.Partner.AnyAsync(p => p.Id == partnerId))
                return Results.NotFound();

            if (string.IsNullOrWhiteSpace(dto.Name))
                return Results.BadRequest("Name darf nicht leer sein.");

            var ap = new Ansprechpartner
            {
                Id = Guid.NewGuid(),
                PartnerId = partnerId,
                Name = dto.Name.Trim(),
                Position = dto.Position?.Trim(),
                Email = dto.Email?.Trim(),
                Telefon = dto.Telefon?.Trim(),
                Notizen = dto.Notizen?.Trim(),
            };
            db.Ansprechpartner.Add(ap);
            await db.SaveChangesAsync();

            return Results.Created($"/crm/partner/{partnerId}/ansprechpartner/{ap.Id}",
                new AnsprechpartnerDto(ap.Id, ap.Name, ap.Position, ap.Email, ap.Telefon, ap.Notizen));
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapPut("/crm/partner/{partnerId:guid}/ansprechpartner/{id:guid}",
            async (Guid partnerId, Guid id, CreateAnsprechpartnerDto dto, ApplicationDbContext db) =>
        {
            var ap = await db.Ansprechpartner
                .FirstOrDefaultAsync(a => a.Id == id && a.PartnerId == partnerId);
            if (ap is null) return Results.NotFound();

            ap.Name = dto.Name.Trim();
            ap.Position = dto.Position?.Trim();
            ap.Email = dto.Email?.Trim();
            ap.Telefon = dto.Telefon?.Trim();
            ap.Notizen = dto.Notizen?.Trim();
            await db.SaveChangesAsync();

            return Results.Ok(new AnsprechpartnerDto(ap.Id, ap.Name, ap.Position, ap.Email, ap.Telefon, ap.Notizen));
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapDelete("/crm/partner/{partnerId:guid}/ansprechpartner/{id:guid}",
            async (Guid partnerId, Guid id, ApplicationDbContext db) =>
        {
            var ap = await db.Ansprechpartner
                .FirstOrDefaultAsync(a => a.Id == id && a.PartnerId == partnerId);
            if (ap is null) return Results.NotFound();
            db.Ansprechpartner.Remove(ap);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("VorstandOrAdmin");

        // -------------------------------------------------------------------------
        // Kontakteinträge
        // -------------------------------------------------------------------------

        app.MapPost("/crm/partner/{partnerId:guid}/kontakt",
            async (Guid partnerId, CreateKontakteintragDto dto, ApplicationDbContext db) =>
        {
            if (!await db.Partner.AnyAsync(p => p.Id == partnerId))
                return Results.NotFound();

            if (string.IsNullOrWhiteSpace(dto.Zusammenfassung))
                return Results.BadRequest("Zusammenfassung darf nicht leer sein.");

            string? apName = null;
            if (dto.AnsprechpartnerId.HasValue)
                apName = await db.Ansprechpartner
                    .Where(a => a.Id == dto.AnsprechpartnerId)
                    .Select(a => a.Name)
                    .FirstOrDefaultAsync();

            var eintrag = new Kontakteintrag
            {
                Id = Guid.NewGuid(),
                PartnerId = partnerId,
                AnsprechpartnerId = dto.AnsprechpartnerId,
                Datum = dto.Datum.ToUniversalTime(),
                Kanal = dto.Kanal,
                Reaktion = dto.Reaktion,
                Zusammenfassung = dto.Zusammenfassung.Trim(),
                NaechsteSchritte = dto.NaechsteSchritte?.Trim(),
            };
            db.Kontakteintraege.Add(eintrag);
            await db.SaveChangesAsync();

            return Results.Created($"/crm/partner/{partnerId}/kontakt/{eintrag.Id}",
                new KontakteintragDto(eintrag.Id, eintrag.AnsprechpartnerId, apName,
                    eintrag.Datum, eintrag.Kanal, eintrag.Reaktion,
                    eintrag.Zusammenfassung, eintrag.NaechsteSchritte, eintrag.CreatedAt));
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapPut("/crm/partner/{partnerId:guid}/kontakt/{id:guid}",
            async (Guid partnerId, Guid id, CreateKontakteintragDto dto, ApplicationDbContext db) =>
        {
            var eintrag = await db.Kontakteintraege
                .Include(k => k.Ansprechpartner)
                .FirstOrDefaultAsync(k => k.Id == id && k.PartnerId == partnerId);
            if (eintrag is null) return Results.NotFound();

            string? apName = null;
            if (dto.AnsprechpartnerId.HasValue)
                apName = await db.Ansprechpartner
                    .Where(a => a.Id == dto.AnsprechpartnerId)
                    .Select(a => a.Name)
                    .FirstOrDefaultAsync();

            eintrag.AnsprechpartnerId = dto.AnsprechpartnerId;
            eintrag.Datum = dto.Datum.ToUniversalTime();
            eintrag.Kanal = dto.Kanal;
            eintrag.Reaktion = dto.Reaktion;
            eintrag.Zusammenfassung = dto.Zusammenfassung.Trim();
            eintrag.NaechsteSchritte = dto.NaechsteSchritte?.Trim();
            await db.SaveChangesAsync();

            return Results.Ok(new KontakteintragDto(eintrag.Id, eintrag.AnsprechpartnerId, apName,
                eintrag.Datum, eintrag.Kanal, eintrag.Reaktion,
                eintrag.Zusammenfassung, eintrag.NaechsteSchritte, eintrag.CreatedAt));
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapDelete("/crm/partner/{partnerId:guid}/kontakt/{id:guid}",
            async (Guid partnerId, Guid id, ApplicationDbContext db) =>
        {
            var eintrag = await db.Kontakteintraege
                .FirstOrDefaultAsync(k => k.Id == id && k.PartnerId == partnerId);
            if (eintrag is null) return Results.NotFound();
            db.Kontakteintraege.Remove(eintrag);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("VorstandOrAdmin");

        return app;
    }
}
