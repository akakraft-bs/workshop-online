using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;

namespace AkaKraft.WebApi;

public static class verbrauchsmaterialApi
{
    public static WebApplication AddverbrauchsmaterialApi(this WebApplication app)
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

        app.MapDelete("/verbrauchsmaterial/{id:guid}", async (Guid id, IVerbrauchsmaterialService verbrauchsmaterialService) =>
        {
            var deleted = await verbrauchsmaterialService.DeleteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("VorstandOrAdmin");

        return app;
    }
}