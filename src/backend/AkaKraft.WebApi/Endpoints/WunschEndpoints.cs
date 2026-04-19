using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;

namespace AkaKraft.WebApi.Endpoints;

internal static class WunschEndpoints
{
    internal static void MapWunschEndpoints(this WebApplication app)
    {
        app.MapGet("/wunsch", async (HttpContext ctx, IWunschService wunschService) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId))
                return Results.Unauthorized();

            return Results.Ok(await wunschService.GetAllAsync(userId));
        }).RequireAuthorization("AnyRole");

        app.MapPost("/wunsch", async (CreateWunschDto dto, HttpContext ctx, IWunschService wunschService) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId))
                return Results.Unauthorized();

            var created = await wunschService.CreateAsync(userId, dto);
            return Results.Created($"/wunsch/{created.Id}", created);
        }).RequireAuthorization("AnyRole");

        app.MapPost("/wunsch/{id:guid}/vote", async (
            Guid id, VoteWunschDto dto, HttpContext ctx, IWunschService wunschService) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId))
                return Results.Unauthorized();

            var result = await wunschService.VoteAsync(id, userId, dto.IsUpvote);
            return result is null
                ? Results.BadRequest("Wunsch nicht gefunden oder bereits abgeschlossen.")
                : Results.Ok(result);
        }).RequireAuthorization("AnyRole");

        app.MapPut("/wunsch/{id:guid}", async (
            Guid id, UpdateWunschDto dto, HttpContext ctx, IWunschService wunschService) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId))
                return Results.Unauthorized();

            var (result, forbidden) = await wunschService.UpdateAsync(id, userId, ctx.IsPrivileged(), dto);

            if (forbidden) return Results.Forbid();
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("AnyRole");

        app.MapPost("/wunsch/{id:guid}/close", async (
            Guid id, CloseWunschDto dto, HttpContext ctx, IWunschService wunschService) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId))
                return Results.Unauthorized();

            var result = await wunschService.CloseAsync(id, userId, dto);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("VorstandOrAdmin");
    }
}
