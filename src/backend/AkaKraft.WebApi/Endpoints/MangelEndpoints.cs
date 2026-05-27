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

        app.MapPatch("/mangel/{id:guid}", async (
            Guid id,
            UpdateMangelContentDto dto,
            HttpContext ctx,
            IMangelService mangelService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var (result, forbidden) = await mangelService.UpdateContentAsync(id, parsedUserId, ctx.IsPrivileged(), dto);

            if (forbidden) return Results.Forbid();
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("AnyRole");

        // ---- Anmerkungen ----

        app.MapPost("/mangel/{id:guid}/anmerkungen", async (
            Guid id,
            CreateMangelAnmerkungDto dto,
            HttpContext ctx,
            IMangelService mangelService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Text))
                return Results.BadRequest("Text darf nicht leer sein.");

            var result = await mangelService.AddAnmerkungAsync(id, parsedUserId, dto);
            return result is null ? Results.NotFound() : Results.Created($"/mangel/{id}/anmerkungen/{result.Id}", result);
        }).RequireAuthorization("AnyRole");

        app.MapPut("/mangel/{id:guid}/anmerkungen/{anmerkungId:guid}", async (
            Guid id,
            Guid anmerkungId,
            UpdateMangelAnmerkungDto dto,
            HttpContext ctx,
            IMangelService mangelService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Text))
                return Results.BadRequest("Text darf nicht leer sein.");

            var (result, forbidden) = await mangelService.UpdateAnmerkungAsync(id, anmerkungId, parsedUserId, ctx.IsPrivileged(), dto);
            if (forbidden) return Results.Forbid();
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("AnyRole");

        app.MapDelete("/mangel/{id:guid}/anmerkungen/{anmerkungId:guid}", async (
            Guid id,
            Guid anmerkungId,
            HttpContext ctx,
            IMangelService mangelService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var (success, forbidden) = await mangelService.DeleteAnmerkungAsync(id, anmerkungId, parsedUserId, ctx.IsPrivileged());
            if (forbidden) return Results.Forbid();
            return success ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("AnyRole");

        return app;
    }
}