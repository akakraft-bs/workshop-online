using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;

namespace AkaKraft.WebApi.Endpoints;

internal static class FeedbackEndpoints
{
    internal static void MapFeedbackEndpoints(this WebApplication app)
    {
        app.MapPost("/feedback", async (CreateFeedbackDto dto, HttpContext ctx, IFeedbackService feedbackService) =>
        {
            if (!ctx.TryGetCurrentUserId(out var userId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Text) || dto.Text.Length > 256)
                return Results.BadRequest("Text muss zwischen 1 und 256 Zeichen lang sein.");

            var result = await feedbackService.CreateAsync(userId, dto);
            return Results.Created($"/admin/feedback/{result.Id}", result);
        }).RequireAuthorization("AnyRole");

        app.MapGet("/admin/feedback", async (IFeedbackService feedbackService) =>
            Results.Ok(await feedbackService.GetAllAsync()))
            .RequireAuthorization("AdminOnly");

        app.MapPatch("/admin/feedback/{id:guid}/status", async (
            Guid id, UpdateFeedbackStatusDto dto, IFeedbackService feedbackService) =>
        {
            var result = await feedbackService.UpdateStatusAsync(id, dto.Status);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("AdminOnly");
    }
}
