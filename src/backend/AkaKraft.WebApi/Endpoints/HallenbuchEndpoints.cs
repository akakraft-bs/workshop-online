using System.Text;
using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;

namespace AkaKraft.WebApi.Endpoints;

internal static class HallenbuchEndpoints
{
    internal static void MapHallenbuchEndpoints(this WebApplication app)
    {
        app.MapGet("/hallenbuch", async (IHallenbuchService service) =>
            Results.Ok(await service.GetAllAsync()))
            .RequireAuthorization("AnyRole");

        app.MapPost("/hallenbuch", async (
            CreateHallenbuchEintragDto dto, HttpContext ctx, IHallenbuchService service) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Description) || dto.Description.Length > 256)
                return Results.BadRequest("Beschreibung muss zwischen 1 und 256 Zeichen lang sein.");

            if (dto.End <= dto.Start)
                return Results.BadRequest("Endzeit muss nach der Startzeit liegen.");

            if (dto.HatGastgeschraubt && dto.GastschraubenArt is null)
                return Results.BadRequest("Zahlungsart für Gastschrauben ist erforderlich.");

            var created = await service.CreateAsync(userId, dto);
            return Results.Created($"/hallenbuch/{created.Id}", created);
        }).RequireAuthorization("AnyRole");

        app.MapPut("/hallenbuch/{id:guid}", async (
            Guid id, UpdateHallenbuchEintragDto dto, HttpContext ctx, IHallenbuchService service) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Description) || dto.Description.Length > 256)
                return Results.BadRequest("Beschreibung muss zwischen 1 und 256 Zeichen lang sein.");

            if (dto.End <= dto.Start)
                return Results.BadRequest("Endzeit muss nach der Startzeit liegen.");

            if (dto.HatGastgeschraubt && dto.GastschraubenArt is null)
                return Results.BadRequest("Zahlungsart für Gastschrauben ist erforderlich.");

            var (result, forbidden) = await service.UpdateAsync(id, userId, ctx.IsPrivileged(), dto);

            if (forbidden) return Results.Forbid();
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("AnyRole");

        app.MapDelete("/hallenbuch/{id:guid}", async (
            Guid id, HttpContext ctx, IHallenbuchService service) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId))
                return Results.Unauthorized();

            var (success, forbidden) = await service.DeleteAsync(id, userId, ctx.IsPrivileged());

            if (forbidden) return Results.Forbid();
            return success ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("AnyRole");

        app.MapGet("/hallenbuch/statistik", async (
            DateTime from, DateTime to, IHallenbuchService service) =>
        {
            var data = (await service.GetStatistikAsync(from, to)).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Name;Eigene Stunden;Gast-Stunden");

            foreach (var row in data)
                sb.AppendLine($"{row.UserName};{row.EigeneStunden:F2};{row.GastStunden:F2}");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"hallenbuch-statistik_{from:yyyy-MM-dd}_{to:yyyy-MM-dd}.csv";

            return Results.File(bytes, "text/csv; charset=utf-8", fileName);
        }).RequireAuthorization("VorstandOrAdmin");
    }
}
