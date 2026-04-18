using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Enums;

namespace AkaKraft.WebApi.Helpers;

public static class AdminroleHelper
{
    internal static async Task EnsureAdminRoleAsync(WebApplication app)
    {
        var adminEmail = app.Configuration["Admin:Email"];
        if (string.IsNullOrWhiteSpace(adminEmail))
            return;

        await using var scope = app.Services.CreateAsyncScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSetup");

        var user = await userService.GetByEmailAsync(adminEmail);
        if (user is null)
        {
            logger.LogInformation(
                "Admin-E-Mail '{Email}' noch nicht registriert – Rolle wird beim ersten Login vergeben.",
                adminEmail);
            return;
        }

        if (!user.Roles.Contains(Role.Admin))
        {
            await userService.AssignRoleAsync(user.Id, Role.Admin);
            logger.LogInformation("Admin-Rolle für '{Email}' vergeben.", adminEmail);
        }
    }
}