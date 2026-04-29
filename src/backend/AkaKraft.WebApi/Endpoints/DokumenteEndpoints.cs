using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;

namespace AkaKraft.WebApi.Endpoints;

internal static class DokumenteEndpoints
{
    internal static WebApplication MapDokumenteEndpoints(this WebApplication app)
    {
        app.MapGet("/verein/dokumente", async (IDokumenteService svc) =>
            Results.Ok(await svc.GetAllAsync()))
            .RequireAuthorization("AnyRole");

        app.MapPost("/verein/dokumente/ordner", async (
            HttpContext ctx,
            CreateOrdnerDto dto,
            IDokumenteService svc) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId)) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest("Name ist erforderlich.");
            return Results.Ok(await svc.CreateOrdnerAsync(userId, dto));
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapDelete("/verein/dokumente/ordner/{id:guid}", async (Guid id, IDokumenteService svc) =>
        {
            var ok = await svc.DeleteOrdnerAsync(id);
            return ok ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapPost("/verein/dokumente", async (
            HttpContext ctx,
            CreateDokumentDto dto,
            IDokumenteService svc) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId)) return Results.Unauthorized();
            return Results.Ok(await svc.CreateDokumentAsync(userId, dto));
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapDelete("/verein/dokumente/{id:guid}", async (Guid id, IDokumenteService svc) =>
        {
            var ok = await svc.DeleteDokumentAsync(id);
            return ok ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("VorstandOrAdmin");

        return app;
    }
}
