using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;

namespace AkaKraft.WebApi.Endpoints;

internal static class MangelEndpoints
{
    internal static WebApplication MapMangelEndpoints(this WebApplication app)
    {
        // -------------------------------------------------------------------------
        // Mängelmelder Endpoints
        // -------------------------------------------------------------------------

        app.MapGet("/mangel", async (IMangelService mangelService) =>
            Results.Ok(await mangelService.GetAllAsync()))
            .RequireAuthorization("AnyRole");

        app.MapPost("/mangel", async (
            CreateMangelDto dto,
            HttpContext ctx,
            IMangelService mangelService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var created = await mangelService.CreateAsync(parsedUserId, dto);
            return Results.Created($"/mangel/{created.Id}", created);
        }).RequireAuthorization("AnyRole");

        app.MapPost("/mangel/{id:guid}/zurueckziehen", async (
            Guid id,
            HttpContext ctx,
            IMangelService mangelService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var (dto, forbidden) = await mangelService.ZurueckziehenAsync(id, parsedUserId);

            if (forbidden) return Results.Forbid();
            return dto is null
                ? Results.BadRequest("Mangel nicht gefunden oder nicht mehr offen.")
                : Results.Ok(dto);
        }).RequireAuthorization("AnyRole");

        app.MapPatch("/mangel/{id:guid}/status", async (
            Guid id,
            UpdateMangelStatusDto dto,
            HttpContext ctx,
            IMangelService mangelService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var updated = await mangelService.UpdateStatusAsync(id, parsedUserId, dto);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).RequireAuthorization("AnyRole");

        return app;
    }
}