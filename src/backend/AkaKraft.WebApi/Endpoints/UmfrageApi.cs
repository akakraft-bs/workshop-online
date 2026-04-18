using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Enums;

namespace AkaKraft.WebApi.Endpoints;

public static class UmfrageApi
{
    public static WebApplication AddUmfrageApi(this WebApplication app)
    {
        // -------------------------------------------------------------------------
        // Umfragen Endpoints
        // -------------------------------------------------------------------------

        app.MapGet("/umfrage", async (HttpContext ctx, IUmfrageService umfrageService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var isPrivileged = ctx.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Any(c => RoleGroups.Vorstand.Select(r => r.ToString()).Contains(c.Value)
                       || c.Value == Role.Admin.ToString());

            return Results.Ok(await umfrageService.GetAllAsync(parsedUserId, isPrivileged));
        }).RequireAuthorization("AnyRole");

        app.MapPost("/umfrage", async (
            CreateUmfrageDto dto,
            HttpContext ctx,
            IUmfrageService umfrageService,
            IPushNotificationService pushService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Question))
                return Results.BadRequest("Frage darf nicht leer sein.");

            if (dto.Options is null || dto.Options.Count < 2)
                return Results.BadRequest("Mindestens 2 Antwortmöglichkeiten sind erforderlich.");

            var created = await umfrageService.CreateAsync(parsedUserId, dto);

            // Notify users with Umfragen preference enabled
            var question = created.Question.Length > 70 ? created.Question[..67] + "…" : created.Question;
            _ = pushService.SendToUsersWithPreferenceAsync(
                p => p.NotifyUmfragen,
                "Neue Umfrage 📊",
                question,
                url: "/umfrage");

            return Results.Created($"/umfrage/{created.Id}", created);
        }).RequireAuthorization("AnyRole");

        app.MapPut("/umfrage/{id:guid}", async (
            Guid id,
            UpdateUmfrageDto dto,
            HttpContext ctx,
            IUmfrageService umfrageService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Question))
                return Results.BadRequest("Frage darf nicht leer sein.");

            if (dto.Options is null || dto.Options.Count < 2)
                return Results.BadRequest("Mindestens 2 Antwortmöglichkeiten sind erforderlich.");

            var isPrivileged = ctx.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Any(c => RoleGroups.Vorstand.Select(r => r.ToString()).Contains(c.Value)
                       || c.Value == Role.Admin.ToString());

            var (result, forbidden) = await umfrageService.UpdateAsync(id, parsedUserId, isPrivileged, dto);

            if (forbidden) return Results.Forbid();
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("AnyRole");

        app.MapDelete("/umfrage/{id:guid}", async (
            Guid id,
            HttpContext ctx,
            IUmfrageService umfrageService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var isPrivileged = ctx.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Any(c => RoleGroups.Vorstand.Select(r => r.ToString()).Contains(c.Value)
                       || c.Value == Role.Admin.ToString());

            var (success, forbidden) = await umfrageService.DeleteAsync(id, parsedUserId, isPrivileged);

            if (forbidden) return Results.Forbid();
            return success ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("AnyRole");

        app.MapPost("/umfrage/{id:guid}/vote", async (
            Guid id,
            VoteUmfrageDto dto,
            HttpContext ctx,
            IUmfrageService umfrageService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var isPrivileged = ctx.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Any(c => RoleGroups.Vorstand.Select(r => r.ToString()).Contains(c.Value)
                       || c.Value == Role.Admin.ToString());

            var (result, error) = await umfrageService.VoteAsync(id, parsedUserId, dto, isPrivileged);

            return error is not null
                ? Results.BadRequest(error)
                : Results.Ok(result);
        }).RequireAuthorization("AnyRole");

        app.MapPost("/umfrage/{id:guid}/close", async (
            Guid id,
            HttpContext ctx,
            IUmfrageService umfrageService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var isPrivileged = ctx.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Any(c => RoleGroups.Vorstand.Select(r => r.ToString()).Contains(c.Value)
                       || c.Value == Role.Admin.ToString());

            var (result, forbidden) = await umfrageService.CloseAsync(id, parsedUserId, isPrivileged);

            if (forbidden) return Results.Forbid();
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("AnyRole");
        return app;
    }
}