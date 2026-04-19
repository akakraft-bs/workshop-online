using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AkaKraft.Domain.Enums;

namespace AkaKraft.WebApi.Endpoints;

internal static class HttpContextExtensions
{
    internal static bool TryGetCurrentUserId(this HttpContext ctx, out Guid userId)
    {
        var raw = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(raw, out userId);
    }

    internal static bool IsPrivileged(this HttpContext ctx) =>
        ctx.User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Any(c => RoleGroups.Vorstand.Select(r => r.ToString()).Contains(c.Value)
                   || c.Value == Role.Admin.ToString());
}
