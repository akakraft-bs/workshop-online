using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;

namespace AkaKraft.WebApi.Endpoints;

internal static class VereinZugangEndpoints
{
    internal static WebApplication MapVereinZugangEndpoints(this WebApplication app)
    {
        app.MapGet("/verein/zugaenge", async (IVereinZugangService svc) =>
            Results.Ok(await svc.GetAllAsync()))
            .RequireAuthorization("AnyRole");

        app.MapPost("/verein/zugaenge", async (CreateVereinZugangDto dto, IVereinZugangService svc) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Anbieter) || string.IsNullOrWhiteSpace(dto.Zugangsdaten))
                return Results.BadRequest("Anbieter und Zugangsdaten sind erforderlich.");
            return Results.Ok(await svc.CreateAsync(dto));
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapPut("/verein/zugaenge/{id:guid}", async (Guid id, UpdateVereinZugangDto dto, IVereinZugangService svc) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Anbieter) || string.IsNullOrWhiteSpace(dto.Zugangsdaten))
                return Results.BadRequest("Anbieter und Zugangsdaten sind erforderlich.");
            var result = await svc.UpdateAsync(id, dto);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapDelete("/verein/zugaenge/{id:guid}", async (Guid id, IVereinZugangService svc) =>
        {
            var ok = await svc.DeleteAsync(id);
            return ok ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("VorstandOrAdmin");

        return app;
    }
}
