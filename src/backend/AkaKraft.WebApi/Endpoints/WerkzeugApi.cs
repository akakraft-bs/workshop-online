using System.CodeDom;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Enums;

namespace AkaKraft.WebApi.Endpoints;

public static class WerkzeugApi
{
    public static WebApplication AddWerkzeugApi(this WebApplication app)
    {
        // -------------------------------------------------------------------------
        // Werkzeug Endpoints
        // -------------------------------------------------------------------------

        app.MapGet("/werkzeug", async (IWerkzeugService werkzeugService) =>
            Results.Ok(await werkzeugService.GetAllAsync()))
            .RequireAuthorization("AnyRole");

        app.MapGet("/werkzeug/categories", async (IWerkzeugService werkzeugService) =>
            Results.Ok(await werkzeugService.GetCategoriesAsync()))
            .RequireAuthorization("AnyRole");

        app.MapPost("/werkzeug", async (CreateWerkzeugDto dto, IWerkzeugService werkzeugService) =>
        {
            var created = await werkzeugService.CreateAsync(dto);
            return Results.Created($"/werkzeug/{created.Id}", created);
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapPut("/werkzeug/{id:guid}", async (Guid id, UpdateWerkzeugDto dto, IWerkzeugService werkzeugService) =>
        {
            var updated = await werkzeugService.UpdateAsync(id, dto);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapDelete("/werkzeug/{id:guid}", async (Guid id, IWerkzeugService werkzeugService) =>
        {
            var deleted = await werkzeugService.DeleteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapPost("/werkzeug/{id:guid}/ausleihen", async (
            Guid id, AusleihenRequestDto dto, HttpContext ctx, IWerkzeugService werkzeugService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var result = await werkzeugService.AusleihenAsync(id, parsedUserId, dto.ExpectedReturnAt);
            return result is null
                ? Results.BadRequest("Werkzeug nicht gefunden oder nicht verfügbar.")
                : Results.Ok(result);
        }).RequireAuthorization("AnyRole");

        app.MapPost("/werkzeug/{id:guid}/zurueckgeben", async (
            Guid id, HttpContext ctx, IWerkzeugService werkzeugService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var isPrivileged = ctx.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Any(c => RoleGroups.Vorstand.Select(r => r.ToString()).Contains(c.Value)
                       || c.Value == Role.Admin.ToString());

            var (dto, forbidden) = await werkzeugService.ZurueckgebenAsync(id, parsedUserId, isPrivileged);

            if (forbidden) return Results.Forbid();
            return dto is null
                ? Results.BadRequest("Werkzeug nicht gefunden oder bereits verfügbar.")
                : Results.Ok(dto);
        }).RequireAuthorization("AnyRole");

        return app;
    }
}