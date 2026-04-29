using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;

namespace AkaKraft.WebApi.Endpoints;

internal static class AufgabeEndpoints
{
    internal static WebApplication MapAufgabeEndpoints(this WebApplication app)
    {
        app.MapGet("/aufgaben", async (IAufgabeService svc) =>
            Results.Ok(await svc.GetAllAsync()))
            .RequireAuthorization("AnyRole");

        app.MapPost("/aufgaben", async (
            CreateAufgabeDto dto,
            HttpContext ctx,
            IAufgabeService svc) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Titel) || string.IsNullOrWhiteSpace(dto.Beschreibung))
                return Results.BadRequest("Titel und Beschreibung sind erforderlich.");

            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedId))
                return Results.Unauthorized();

            var created = await svc.CreateAsync(parsedId, dto);
            return Results.Created($"/aufgaben/{created.Id}", created);
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapPut("/aufgaben/{id:guid}", async (
            Guid id,
            UpdateAufgabeDto dto,
            IAufgabeService svc) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Titel) || string.IsNullOrWhiteSpace(dto.Beschreibung))
                return Results.BadRequest("Titel und Beschreibung sind erforderlich.");

            var result = await svc.UpdateAsync(id, dto);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapDelete("/aufgaben/{id:guid}", async (Guid id, IAufgabeService svc) =>
        {
            var ok = await svc.DeleteAsync(id);
            return ok ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("VorstandOrAdmin");

        return app;
    }
}
