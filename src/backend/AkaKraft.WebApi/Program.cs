using System.Security.Claims;
using System.Text;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Enums;
using AkaKraft.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace AkaKraft.WebApi;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddInfrastructure(builder.Configuration);

        // -------------------------------------------------------------------------
        // Authentication: Cookies (für Google-Callback) + Google OAuth + JWT Bearer
        // -------------------------------------------------------------------------
        builder.Services.Configure<CookiePolicyOptions>(options =>
        {
            options.MinimumSameSitePolicy = SameSiteMode.Lax;
            options.Secure = CookieSecurePolicy.SameAsRequest;
        });

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            })
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
                options.CallbackPath = "/auth/callback/google";
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var jwtConfig = builder.Configuration.GetSection("Authentication:Jwt");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtConfig["Issuer"],
                    ValidAudience = jwtConfig["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtConfig["Key"]!)),
                };
            });

        // -------------------------------------------------------------------------
        // Authorization Policies
        // -------------------------------------------------------------------------
        builder.Services.AddAuthorization(options =>
        {
            // Basis-Policy für alle geschützten API-Endpoints: JWT Bearer
            options.AddPolicy("JwtApi", policy =>
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                      .RequireAuthenticatedUser());

            // Jeder eingeloggte Nutzer (außer None)
            options.AddPolicy("AnyRole", policy =>
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(ctx =>
                          ctx.User.Claims
                             .Where(c => c.Type == ClaimTypes.Role)
                             .Any(c => c.Value != Role.None.ToString())));

            // Beliebige Vorstandsrolle
            options.AddPolicy("VorstandOnly", policy =>
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(ctx =>
                          ctx.User.Claims
                             .Where(c => c.Type == ClaimTypes.Role)
                             .Any(c => RoleGroups.Vorstand
                                 .Select(r => r.ToString())
                                 .Contains(c.Value))));

            // Nur Chairman oder ViceChairman
            options.AddPolicy("ChairmanOnly", policy =>
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireRole(Role.Chairman.ToString(), Role.ViceChairman.ToString()));

            // Vollzugriff
            options.AddPolicy("AdminOnly", policy =>
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireRole(Role.Admin.ToString()));
        });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
                policy.WithOrigins(builder.Configuration["Frontend:BaseUrl"]!)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials());
        });

        var app = builder.Build();

        app.UseCors("Frontend");
        app.UseCookiePolicy();
        app.UseAuthentication();
        app.UseAuthorization();

        // -------------------------------------------------------------------------
        // Auth Endpoints
        // -------------------------------------------------------------------------

        // Schritt 1: Frontend leitet hierher weiter → Google-Login startet
        app.MapGet("/auth/login/google", (HttpContext ctx) =>
            Results.Challenge(
                new AuthenticationProperties { RedirectUri = "/auth/callback" },
                [GoogleDefaults.AuthenticationScheme]));

        // Schritt 2: Google-Callback → Nutzer anlegen/laden → JWT erzeugen → Frontend weiterleiten
        app.MapGet("/auth/callback", async (HttpContext ctx, IAuthService authService, IConfiguration config) =>
        {
            var result = await ctx.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return Results.Unauthorized();

            var principal = result.Principal;
            var googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var email    = principal.FindFirstValue(ClaimTypes.Email)!;
            var name     = principal.FindFirstValue(ClaimTypes.Name)!;
            var picture  = principal.FindFirstValue("picture");

            var authResult = await authService.HandleGoogleCallbackAsync(googleId, email, name, picture);

            var frontendUrl = config["Frontend:BaseUrl"];
            return Results.Redirect($"{frontendUrl}/auth/callback?token={authResult.Token}");
        });

        // Gibt den aktuell eingeloggten Nutzer zurück (JWT-geschützt)
        app.MapGet("/auth/me", async (HttpContext ctx, IUserService userService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var id))
                return Results.Unauthorized();

            var user = await userService.GetByIdAsync(id);
            return user is null ? Results.Unauthorized() : Results.Ok(user);
        }).RequireAuthorization("JwtApi");

        // -------------------------------------------------------------------------
        // User-Management (Admin)
        // -------------------------------------------------------------------------

        app.MapGet("/users", async (IUserService userService) =>
            Results.Ok(await userService.GetAllAsync()))
            .RequireAuthorization("AdminOnly");

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

        app.Run();
    }
}
