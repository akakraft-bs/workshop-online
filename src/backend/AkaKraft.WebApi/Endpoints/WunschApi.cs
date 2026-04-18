using System.CodeDom;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Enums;

namespace AkaKraft.WebApi.Endpoints;
public static class WunschApi
{
    public static WebApplication AddWunschApi(this WebApplication app)
    {
        // -------------------------------------------------------------------------
        // Wunschliste Endpoints
        // -------------------------------------------------------------------------

        app.MapGet("/wunsch", async (HttpContext ctx, IWunschService wunschService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            return Results.Ok(await wunschService.GetAllAsync(parsedUserId));
        }).RequireAuthorization("AnyRole");

        app.MapPost("/wunsch", async (
            CreateWunschDto dto,
            HttpContext ctx,
            IWunschService wunschService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var created = await wunschService.CreateAsync(parsedUserId, dto);
            return Results.Created($"/wunsch/{created.Id}", created);
        }).RequireAuthorization("AnyRole");

        app.MapPost("/wunsch/{id:guid}/vote", async (
            Guid id,
            VoteWunschDto dto,
            HttpContext ctx,
            IWunschService wunschService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var result = await wunschService.VoteAsync(id, parsedUserId, dto.IsUpvote);
            return result is null
                ? Results.BadRequest("Wunsch nicht gefunden oder bereits abgeschlossen.")
                : Results.Ok(result);
        }).RequireAuthorization("AnyRole");

        app.MapPut("/wunsch/{id:guid}", async (
            Guid id,
            UpdateWunschDto dto,
            HttpContext ctx,
            IWunschService wunschService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var isPrivileged = ctx.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Any(c => RoleGroups.Vorstand.Select(r => r.ToString()).Contains(c.Value)
                       || c.Value == Role.Admin.ToString());

            var (result, forbidden) = await wunschService.UpdateAsync(id, parsedUserId, isPrivileged, dto);

            if (forbidden) return Results.Forbid();
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("AnyRole");

        app.MapPost("/wunsch/{id:guid}/close", async (
            Guid id,
            CloseWunschDto dto,
            HttpContext ctx,
            IWunschService wunschService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var result = await wunschService.CloseAsync(id, parsedUserId, dto);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("VorstandOrAdmin");

        return app;
    }
}