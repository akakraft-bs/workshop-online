using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;

namespace AkaKraft.WebApi.Endpoints;

internal static class VereinInfoEndpoints
{
    internal static WebApplication MapVereinInfoEndpoints(this WebApplication app)
    {
        app.MapGet("/verein/info", async (IVereinInfoService svc) =>
            Results.Ok(await svc.GetAsync()))
            .RequireAuthorization("AnyRole");

        app.MapPost("/verein/info/schluessel", async (
            HttpContext ctx,
            CreateSchluesselhinterlegungDto dto,
            IVereinInfoService svc) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Address))
                return Results.BadRequest("Name und Adresse sind erforderlich.");
            return Results.Ok(await svc.CreateSchluesselhinterlegungAsync(dto));
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapPut("/verein/info/schluessel/{id:guid}", async (
            Guid id,
            UpdateSchluesselhinterlegungDto dto,
            IVereinInfoService svc) =>
        {
            var result = await svc.UpdateSchluesselhinterlegungAsync(id, dto);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapDelete("/verein/info/schluessel/{id:guid}", async (Guid id, IVereinInfoService svc) =>
        {
            var ok = await svc.DeleteSchluesselhinterlegungAsync(id);
            return ok ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("VorstandOrAdmin");

        return app;
    }
}
