using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;

namespace AkaKraft.WebApi.Endpoints;

public static class FeedbackApi
{
    public static WebApplication AddFeedbackApi(this WebApplication app)
    {
        // -------------------------------------------------------------------------
        // Feedback Endpoints
        // -------------------------------------------------------------------------

        // Nutzer sendet Feedback (jeder freigeschaltete Nutzer)
        app.MapPost("/feedback", async (CreateFeedbackDto dto, HttpContext ctx, IFeedbackService feedbackService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Text) || dto.Text.Length > 256)
                return Results.BadRequest("Text muss zwischen 1 und 256 Zeichen lang sein.");

            var result = await feedbackService.CreateAsync(parsedUserId, dto);
            return Results.Created($"/admin/feedback/{result.Id}", result);
        }).RequireAuthorization("AnyRole");

        return app;
    }
}