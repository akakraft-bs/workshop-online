using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;

namespace AkaKraft.WebApi.Endpoints;

internal static class UmfrageEndpoints
{
    internal static void MapUmfrageEndpoints(this WebApplication app)
    {
        app.MapGet("/umfrage", async (HttpContext ctx, IUmfrageService umfrageService) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId))
                return Results.Unauthorized();

            return Results.Ok(await umfrageService.GetAllAsync(userId, ctx.IsPrivileged()));
        }).RequireAuthorization("AnyRole");

        app.MapPost("/umfrage", async (
            CreateUmfrageDto dto, HttpContext ctx,
            IUmfrageService umfrageService, IPushNotificationService pushService) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Question))
                return Results.BadRequest("Frage darf nicht leer sein.");

            if (dto.Options is null || dto.Options.Count < 2)
                return Results.BadRequest("Mindestens 2 Antwortmöglichkeiten sind erforderlich.");

            var created = await umfrageService.CreateAsync(userId, dto);

            var question = created.Question.Length > 70 ? created.Question[..67] + "…" : created.Question;
            _ = pushService.SendToUsersWithPreferenceAsync(
                p => p.NotifyUmfragen, "Neue Umfrage 📊", question, url: "/umfrage");

            return Results.Created($"/umfrage/{created.Id}", created);
        }).RequireAuthorization("AnyRole");

        app.MapPut("/umfrage/{id:guid}", async (
            Guid id, UpdateUmfrageDto dto, HttpContext ctx, IUmfrageService umfrageService) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Question))
                return Results.BadRequest("Frage darf nicht leer sein.");

            if (dto.Options is null || dto.Options.Count < 2)
                return Results.BadRequest("Mindestens 2 Antwortmöglichkeiten sind erforderlich.");

            var (result, forbidden) = await umfrageService.UpdateAsync(id, userId, ctx.IsPrivileged(), dto);

            if (forbidden) return Results.Forbid();
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("AnyRole");

        app.MapDelete("/umfrage/{id:guid}", async (
            Guid id, HttpContext ctx, IUmfrageService umfrageService) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId))
                return Results.Unauthorized();

            var (success, forbidden) = await umfrageService.DeleteAsync(id, userId, ctx.IsPrivileged());

            if (forbidden) return Results.Forbid();
            return success ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("AnyRole");

        app.MapPost("/umfrage/{id:guid}/vote", async (
            Guid id, VoteUmfrageDto dto, HttpContext ctx, IUmfrageService umfrageService) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId))
                return Results.Unauthorized();

            var (result, error) = await umfrageService.VoteAsync(id, userId, dto, ctx.IsPrivileged());

            return error is not null ? Results.BadRequest(error) : Results.Ok(result);
        }).RequireAuthorization("AnyRole");

        app.MapPost("/umfrage/{id:guid}/close", async (
            Guid id, HttpContext ctx, IUmfrageService umfrageService) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId))
                return Results.Unauthorized();

            var (result, forbidden) = await umfrageService.CloseAsync(id, userId, ctx.IsPrivileged());

            if (forbidden) return Results.Forbid();
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("AnyRole");
    }
}
