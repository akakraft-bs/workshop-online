using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.WebApi.Endpoints;

namespace AkaKraft.WebApi;

internal static class VerbrauchsmaterialEndlpoints
{
    internal static WebApplication MapVerbrauchsmaterialEndpoints(this WebApplication app)
    {
        // -------------------------------------------------------------------------
        // Verbrauchsmaterial Endpoints
        // -------------------------------------------------------------------------

        app.MapGet("/verbrauchsmaterial", async (IVerbrauchsmaterialService verbrauchsmaterialService) =>
            Results.Ok(await verbrauchsmaterialService.GetAllAsync()))
            .RequireAuthorization("AnyRole");

        app.MapGet("/verbrauchsmaterial/categories", async (IVerbrauchsmaterialService verbrauchsmaterialService) =>
            Results.Ok(await verbrauchsmaterialService.GetCategoriesAsync()))
            .RequireAuthorization("AnyRole");

        app.MapGet("/verbrauchsmaterial/units", async (IVerbrauchsmaterialService verbrauchsmaterialService) =>
            Results.Ok(await verbrauchsmaterialService.GetUnitsAsync()))
            .RequireAuthorization("AnyRole");

        app.MapPost("/verbrauchsmaterial", async (CreateVerbrauchsmaterialDto dto, IVerbrauchsmaterialService verbrauchsmaterialService) =>
        {
            var created = await verbrauchsmaterialService.CreateAsync(dto);
            return Results.Created($"/verbrauchsmaterial/{created.Id}", created);
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapPut("/verbrauchsmaterial/{id:guid}", async (Guid id, UpdateVerbrauchsmaterialDto dto, IVerbrauchsmaterialService verbrauchsmaterialService) =>
        {
            var updated = await verbrauchsmaterialService.UpdateAsync(id, dto);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapPatch("/verbrauchsmaterial/{id:guid}/quantity", async (Guid id, AdjustQuantityDto dto, IVerbrauchsmaterialService verbrauchsmaterialService) =>
        {
            var updated = await verbrauchsmaterialService.AdjustQuantityAsync(id, dto.Delta);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapPost("/verbrauchsmaterial/{id:guid}/nachbestellen", async (
            Guid id, HttpContext ctx,
            IVerbrauchsmaterialService verbrauchsmaterialService,
            IUserService userService, IUserPreferencesService prefsService) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId)) return Results.Unauthorized();
            var user = await userService.GetByIdAsync(userId);
            if (user is null) return Results.Unauthorized();
            var prefs = await prefsService.GetAsync(userId);
            var displayName = !string.IsNullOrWhiteSpace(prefs.DisplayName) ? prefs.DisplayName : user.Name;

            var updated = await verbrauchsmaterialService.SetNachbestelltAsync(id, displayName);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapDelete("/verbrauchsmaterial/{id:guid}", async (Guid id, IVerbrauchsmaterialService verbrauchsmaterialService) =>
        {
            var deleted = await verbrauchsmaterialService.DeleteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("VorstandOrAdmin");

        return app;
    }
}