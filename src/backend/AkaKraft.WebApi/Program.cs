using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using AkaKraft.Application.DTOs;
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

        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

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
                // Sicherstellt dass sub → ClaimTypes.NameIdentifier gemappt wird
                options.MapInboundClaims = true;
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

            // Vorstand oder Admin (z. B. für Inventarverwaltung)
            options.AddPolicy("VorstandOrAdmin", policy =>
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(ctx =>
                          ctx.User.Claims
                             .Where(c => c.Type == ClaimTypes.Role)
                             .Any(c => RoleGroups.Vorstand
                                 .Select(r => r.ToString())
                                 .Contains(c.Value)
                                 || c.Value == Role.Admin.ToString())));
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

        // wwwroot/uploads sicherstellen
        var wwwroot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
        Directory.CreateDirectory(Path.Combine(wwwroot, "uploads", "werkzeug"));

        app.UseCors("Frontend");
        app.UseStaticFiles();
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
            // MapInboundClaims=true mappt sub → NameIdentifier; Fallback für ältere Token
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
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

        // -------------------------------------------------------------------------
        // Upload Endpoints
        // -------------------------------------------------------------------------

        app.MapPost("/uploads/werkzeug", async (IFormFile file, IUploadService uploadService) =>
        {
            try
            {
                var model = new FileUploadModel(file.OpenReadStream(), file.FileName, file.ContentType, file.Length);
                var url = await uploadService.SaveAsync(model);
                return Results.Ok(new { url });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireAuthorization("VorstandOrAdmin")
          .DisableAntiforgery();

        // -------------------------------------------------------------------------
        // Werkzeug Endpoints
        // -------------------------------------------------------------------------

        app.MapGet("/werkzeug", async (IWerkzeugService werkzeugService) =>
            Results.Ok(await werkzeugService.GetAllAsync()))
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

        // -------------------------------------------------------------------------
        // Verbrauchsmaterial Endpoints
        // -------------------------------------------------------------------------

        app.MapGet("/verbrauchsmaterial", async (IVerbrauchsmaterialService verbrauchsmaterialService) =>
            Results.Ok(await verbrauchsmaterialService.GetAllAsync()))
            .RequireAuthorization("AnyRole");

        app.Run();
    }
}
