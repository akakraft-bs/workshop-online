using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;

namespace AkaKraft.WebApi.Endpoints;

internal static class ProjektEndpoints
{
    internal static WebApplication MapProjektEndpoints(this WebApplication app)
    {
        app.MapGet("/verein/projekte", async (IProjektService svc) =>
            Results.Ok(await svc.GetAllAsync()))
            .RequireAuthorization("AnyRole");

        app.MapPost("/verein/projekte", async (
            HttpContext ctx,
            CreateProjektDto dto,
            IProjektService svc) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId)) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest("Name ist erforderlich.");
            return Results.Ok(await svc.CreateAsync(userId, dto));
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapPut("/verein/projekte/{id:guid}", async (
            Guid id,
            UpdateProjektDto dto,
            IProjektService svc) =>
        {
            var result = await svc.UpdateAsync(id, dto);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapDelete("/verein/projekte/{id:guid}", async (Guid id, IProjektService svc) =>
        {
            var ok = await svc.DeleteAsync(id);
            return ok ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("VorstandOrAdmin");

        return app;
    }
}
