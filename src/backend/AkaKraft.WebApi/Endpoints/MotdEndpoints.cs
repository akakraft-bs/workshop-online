using AkaKraft.Application.DTOs;
using AkaKraft.Domain.Entities;
using AkaKraft.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.WebApi.Endpoints;

internal static class MotdEndpoints
{
    internal static WebApplication MapMotdEndpoints(this WebApplication app)
    {
        app.MapGet("/motd", async (ApplicationDbContext db) =>
        {
            var motd = await db.Motds.FirstOrDefaultAsync();
            if (motd is null) return Results.NoContent();
            return Results.Ok(new MotdDto(motd.Id, motd.Message, motd.Severity, motd.UpdatedAt ?? motd.CreatedAt));
        }).RequireAuthorization("AnyRole");

        app.MapPut("/motd", async (SetMotdDto dto, ApplicationDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Message))
                return Results.BadRequest("Nachricht darf nicht leer sein.");

            var motd = await db.Motds.FirstOrDefaultAsync();
            if (motd is null)
            {
                motd = new Motd { Id = Guid.NewGuid() };
                db.Motds.Add(motd);
            }

            motd.Message = dto.Message.Trim();
            motd.Severity = dto.Severity;
            await db.SaveChangesAsync();

            return Results.Ok(new MotdDto(motd.Id, motd.Message, motd.Severity, motd.UpdatedAt ?? motd.CreatedAt));
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapDelete("/motd", async (ApplicationDbContext db) =>
        {
            var motd = await db.Motds.FirstOrDefaultAsync();
            if (motd is null) return Results.NoContent();

            db.Motds.Remove(motd);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization("VorstandOrAdmin");

        return app;
    }
}
