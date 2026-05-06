using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Enums;
using AkaKraft.Infrastructure.Data;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.EntityFrameworkCore;

namespace AkaKraft.WebApi.Endpoints;

internal static class UserEndpoints
{
    internal static WebApplication MapUserEndpoints(this WebApplication app)
    {
        // -------------------------------------------------------------------------
        // User-Management (Admin)
        // -------------------------------------------------------------------------

        app.MapGet("/users", async (IUserService userService) =>
            Results.Ok(await userService.GetAllAsync()))
            .RequireAuthorization("AdminOnly");

        app.MapGet("/users/assignable", async (IUserService userService) =>
        {
            var users = await userService.GetAllAsync();
            return Results.Ok(users.Select(u => new { u.Id, Name = u.DisplayName ?? u.Name }));
        }).RequireAuthorization("VorstandOrAdmin");

        app.MapPost("/users/{userId:guid}/roles/{role}", async (
            Guid userId, string role, IUserService userService) =>
        {
            if (!Enum.TryParse<Role>(role, ignoreCase: true, out var parsedRole))
                return Results.BadRequest($"Ungültige Rolle: {role}");

            var user = await userService.AssignRoleAsync(userId, parsedRole);
            return Results.Ok(user);
        }).RequireAuthorization("AdminOnly");

        app.MapDelete("/users/{userId:guid}/roles/{role}", async (
            Guid userId, string role, IUserService userService) =>
        {
            if (!Enum.TryParse<Role>(role, ignoreCase: true, out var parsedRole))
                return Results.BadRequest($"Ungültige Rolle: {role}");

            var user = await userService.RemoveRoleAsync(userId, parsedRole);
            return Results.Ok(user);
        }).RequireAuthorization("AdminOnly");

        // -------------------------------------------------------------------------
        // User Preferences Endpoints
        // -------------------------------------------------------------------------

        app.MapGet("/users/me/badges", async (HttpContext ctx, ApplicationDbContext db) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var id))
                return Results.Unauthorized();

            var isAdmin = ctx.User.Claims.Any(c =>
                c is { Type: "roles" or "role" } && c.Value == Role.Admin.ToString());

            var votedUmfrageIds = await db.UmfrageAntworten
                .Where(a => a.UserId == id)
                .Select(a => a.UmfrageId)
                .Distinct()
                .ToListAsync();

            var pendingUmfragen = await db.Umfragen
                .CountAsync(u => u.Status == UmfrageStatus.Offen && !votedUmfrageIds.Contains(u.Id));

            var openMaengel = await db.Maengel
                .CountAsync(m => m.Status == MangelStatus.Offen || m.Status == MangelStatus.Kenntnisgenommen);

            var lowStock = await db.Verbrauchsmaterialien
                .CountAsync(v => v.MinQuantity != null && v.Quantity <= v.MinQuantity);

            var unseenFeedback = isAdmin
                ? await db.Feedbacks.CountAsync(f => f.Status == FeedbackStatus.New)
                : 0;

            return Results.Ok(new BadgesDto(pendingUmfragen, openMaengel, lowStock, unseenFeedback));
        }).RequireAuthorization("AnyRole");

        app.MapGet("/users/me/preferences", async (HttpContext ctx, IUserPreferencesService prefsService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var id))
                return Results.Unauthorized();

            return Results.Ok(await prefsService.GetAsync(id));
        }).RequireAuthorization("JwtApi");

        app.MapPut("/users/me/preferences", async (
            HttpContext ctx,
            UpdateUserPreferencesDto dto,
            IUserPreferencesService prefsService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var id))
                return Results.Unauthorized();

            return Results.Ok(await prefsService.UpdateAsync(id, dto));
        }).RequireAuthorization("JwtApi");

        return app;
    }
}