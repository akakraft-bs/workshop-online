using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using AkaKraft.Application.DTOs;
using AkaKraft.Application.Interfaces;
using AkaKraft.Domain.Enums;
using AkaKraft.Infrastructure;
using AkaKraft.Infrastructure.Data;
using AkaKraft.WebApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AkaKraft.WebApi;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Lokale Overrides (gitignored): appsettings.Development.local.json etc.
        builder.Configuration.AddJsonFile(
            $"appsettings.{builder.Environment.EnvironmentName}.local.json",
            optional: true,
            reloadOnChange: true);

        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddHostedService<UmfrageDeadlineBackgroundService>();

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

        // Allen Proxies im Cluster vertrauen (NPM → Traefik → nginx → Backend).
        // Nötig damit X-Forwarded-Proto: https vom nginx akzeptiert wird.
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        var app = builder.Build();

        // Ausstehende EF Core Migrationen beim Start automatisch anwenden
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
        }

        // MinIO-Bucket beim Start anlegen und auf öffentlichen Lesezugriff setzen
        await EnsureMinioReadyAsync(app);

        // Sicherstellen dass der konfigurierte Admin die Admin-Rolle hat
        await EnsureAdminRoleAsync(app);

        // Nginx leitet /api/... an das Backend weiter (ohne Prefix zu strippen).
        // UsePathBase entfernt /api aus dem Pfad und fügt es bei URL-Konstruktionen
        // (z. B. OAuth redirect_uri) automatisch wieder hinzu.
        app.UsePathBase("/api");

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
                new AuthenticationProperties { RedirectUri = $"{ctx.Request.PathBase}/auth/callback" },
                [GoogleDefaults.AuthenticationScheme]));

        // Schritt 2: Google-Callback → Nutzer anlegen/laden → JWT + Refresh-Token erzeugen → Frontend weiterleiten
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
            var refreshToken = await authService.CreateRefreshTokenAsync(authResult.User.Id);

            ctx.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                MaxAge = TimeSpan.FromDays(30),
                Path = "/api/auth",
            });

            var frontendUrl = config["Frontend:BaseUrl"];
            return Results.Redirect($"{frontendUrl}/auth/callback?token={authResult.Token}");
        });

        // JWT per Refresh-Token erneuern (Cookie wird automatisch mitgesendet)
        app.MapPost("/auth/refresh", async (HttpContext ctx, IAuthService authService) =>
        {
            var refreshToken = ctx.Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(refreshToken))
                return Results.Unauthorized();

            var result = await authService.UseRefreshTokenAsync(refreshToken);
            if (result is null)
            {
                ctx.Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/api/auth" });
                return Results.Unauthorized();
            }

            ctx.Response.Cookies.Append("refresh_token", result.RefreshToken!, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                MaxAge = TimeSpan.FromDays(30),
                Path = "/api/auth",
            });

            return Results.Ok(new { token = result.Token, expiresAt = result.ExpiresAt });
        });

        // Abmelden: Refresh-Token widerrufen und Cookie löschen
        app.MapPost("/auth/logout", async (HttpContext ctx, IAuthService authService) =>
        {
            var refreshToken = ctx.Request.Cookies["refresh_token"];
            if (!string.IsNullOrEmpty(refreshToken))
                await authService.RevokeRefreshTokenAsync(refreshToken);

            ctx.Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/api/auth" });
            return Results.Ok();
        }).RequireAuthorization("JwtApi");

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
        // User Preferences Endpoints
        // -------------------------------------------------------------------------

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

        app.MapGet("/werkzeug/categories", async (IWerkzeugService werkzeugService) =>
            Results.Ok(await werkzeugService.GetCategoriesAsync()))
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

        app.MapPost("/uploads/verbrauchsmaterial", async (IFormFile file, IUploadService uploadService) =>
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

        app.MapPost("/uploads/mangel", async (IFormFile file, IUploadService uploadService) =>
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
        }).RequireAuthorization("AnyRole")
          .DisableAntiforgery();

        // -------------------------------------------------------------------------
        // Mängelmelder Endpoints
        // -------------------------------------------------------------------------

        app.MapGet("/mangel", async (IMangelService mangelService) =>
            Results.Ok(await mangelService.GetAllAsync()))
            .RequireAuthorization("AnyRole");

        app.MapPost("/mangel", async (
            CreateMangelDto dto,
            HttpContext ctx,
            IMangelService mangelService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var created = await mangelService.CreateAsync(parsedUserId, dto);
            return Results.Created($"/mangel/{created.Id}", created);
        }).RequireAuthorization("AnyRole");

        app.MapPost("/mangel/{id:guid}/zurueckziehen", async (
            Guid id,
            HttpContext ctx,
            IMangelService mangelService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var (dto, forbidden) = await mangelService.ZurueckziehenAsync(id, parsedUserId);

            if (forbidden) return Results.Forbid();
            return dto is null
                ? Results.BadRequest("Mangel nicht gefunden oder nicht mehr offen.")
                : Results.Ok(dto);
        }).RequireAuthorization("AnyRole");

        app.MapPatch("/mangel/{id:guid}/status", async (
            Guid id,
            UpdateMangelStatusDto dto,
            HttpContext ctx,
            IMangelService mangelService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var updated = await mangelService.UpdateStatusAsync(id, parsedUserId, dto);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).RequireAuthorization("VorstandOrAdmin");

        // -------------------------------------------------------------------------
        // Wunschliste Endpoints
        // -------------------------------------------------------------------------

        app.MapGet("/wunsch", async (HttpContext ctx, IWunschService wunschService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            return Results.Ok(await wunschService.GetAllAsync(parsedUserId));
        }).RequireAuthorization("AnyRole");

        app.MapPost("/wunsch", async (
            CreateWunschDto dto,
            HttpContext ctx,
            IWunschService wunschService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var created = await wunschService.CreateAsync(parsedUserId, dto);
            return Results.Created($"/wunsch/{created.Id}", created);
        }).RequireAuthorization("AnyRole");

        app.MapPost("/wunsch/{id:guid}/vote", async (
            Guid id,
            VoteWunschDto dto,
            HttpContext ctx,
            IWunschService wunschService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var result = await wunschService.VoteAsync(id, parsedUserId, dto.IsUpvote);
            return result is null
                ? Results.BadRequest("Wunsch nicht gefunden oder bereits abgeschlossen.")
                : Results.Ok(result);
        }).RequireAuthorization("AnyRole");

        app.MapPut("/wunsch/{id:guid}", async (
            Guid id,
            UpdateWunschDto dto,
            HttpContext ctx,
            IWunschService wunschService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var isPrivileged = ctx.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Any(c => RoleGroups.Vorstand.Select(r => r.ToString()).Contains(c.Value)
                       || c.Value == Role.Admin.ToString());

            var (result, forbidden) = await wunschService.UpdateAsync(id, parsedUserId, isPrivileged, dto);

            if (forbidden) return Results.Forbid();
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("AnyRole");

        app.MapPost("/wunsch/{id:guid}/close", async (
            Guid id,
            CloseWunschDto dto,
            HttpContext ctx,
            IWunschService wunschService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var result = await wunschService.CloseAsync(id, parsedUserId, dto);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("VorstandOrAdmin");

        // -------------------------------------------------------------------------
        // Push-Notification Token Endpoints
        // -------------------------------------------------------------------------

        // FCM-Token für dieses Gerät registrieren
        app.MapPost("/push/tokens", async (
            HttpContext ctx,
            RegisterFcmTokenDto dto,
            ApplicationDbContext db) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Token))
                return Results.BadRequest("Token darf nicht leer sein.");

            // Upsert: Token existiert bereits → RegisteredAt aktualisieren
            var existing = await db.FcmTokens
                .FirstOrDefaultAsync(t => t.Token == dto.Token);

            if (existing is null)
            {
                db.FcmTokens.Add(new AkaKraft.Domain.Entities.FcmToken
                {
                    UserId = parsedUserId,
                    Token = dto.Token,
                });
            }
            else
            {
                existing.UserId = parsedUserId;
                existing.RegisteredAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
            return Results.Ok();
        }).RequireAuthorization("JwtApi");

        // FCM-Token für dieses Gerät entfernen
        app.MapDelete("/push/tokens/{token}", async (
            string token,
            HttpContext ctx,
            ApplicationDbContext db) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var existing = await db.FcmTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.UserId == parsedUserId);

            if (existing is not null)
            {
                db.FcmTokens.Remove(existing);
                await db.SaveChangesAsync();
            }

            return Results.Ok();
        }).RequireAuthorization("JwtApi");

        // Test-Notification an einen oder alle Nutzer senden (Admin)
        app.MapPost("/admin/push/test", async (
            SendTestPushDto dto,
            IPushNotificationService pushService) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Body))
                return Results.BadRequest("Titel und Text dürfen nicht leer sein.");

            if (dto.UserId.HasValue)
                await pushService.SendToUserAsync(dto.UserId.Value, dto.Title, dto.Body);
            else
                await pushService.SendToUsersWithPreferenceAsync(_ => true, dto.Title, dto.Body);

            return Results.Ok();
        }).RequireAuthorization("AdminOnly");

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

        // Admin: alle Feedbacks abrufen
        app.MapGet("/admin/feedback", async (IFeedbackService feedbackService) =>
            Results.Ok(await feedbackService.GetAllAsync()))
            .RequireAuthorization("AdminOnly");

        // Admin: Status eines Feedbacks aktualisieren
        app.MapPatch("/admin/feedback/{id:guid}/status", async (
            Guid id, UpdateFeedbackStatusDto dto, IFeedbackService feedbackService) =>
        {
            var result = await feedbackService.UpdateStatusAsync(id, dto.Status);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("AdminOnly");

        // -------------------------------------------------------------------------
        // Calendar Config Endpoints (Admin)
        // -------------------------------------------------------------------------

        // Alle konfigurierten Kalender abrufen (optional nach Typ filtern)
        app.MapGet("/calendar/configs", async (ICalendarConfigService configService, string? type) =>
        {
            var all = await configService.GetAllAsync();
            if (!string.IsNullOrWhiteSpace(type))
                all = all.Where(c => string.Equals(c.CalendarType, type, StringComparison.OrdinalIgnoreCase));
            return Results.Ok(all);
        }).RequireAuthorization("AnyRole");

        // Verfügbare Google-Kalender + aktueller DB-Config (Admin)
        app.MapGet("/admin/calendar/available", async (
            ICalendarService calendarService,
            ICalendarConfigService configService) =>
        {
            var available = await calendarService.GetAvailableCalendarsAsync();
            var configs = (await configService.GetAllAsync()).ToDictionary(c => c.GoogleCalendarId);
            var availableIds = available.Select(a => a.GoogleCalendarId).ToHashSet();

            var merged = available.Select(a => new AvailableCalendarDto(
                a.GoogleCalendarId,
                a.Name,
                a.Description,
                configs.GetValueOrDefault(a.GoogleCalendarId)
            )).ToList();

            // Auch DB-konfigurierte Kalender einbeziehen, die nicht im Google-CalendarList sind
            foreach (var (id, cfg) in configs)
            {
                if (!availableIds.Contains(id))
                    merged.Add(new AvailableCalendarDto(id, cfg.Name, null, cfg));
            }

            return Results.Ok(merged);
        }).RequireAuthorization("AdminOnly");

        // Service Account bei einem Kalender anmelden (damit er in CalendarList erscheint)
        app.MapPost("/admin/calendar/subscribe", async (
            SubscribeCalendarDto dto,
            ICalendarService calendarService) =>
        {
            var result = await calendarService.SubscribeCalendarAsync(dto.CalendarId);
            if (result is null)
                return Results.Problem("Kalender konnte nicht abonniert werden. Prüfe, ob der Service Account Zugriff hat.");
            return Results.Ok(result);
        }).RequireAuthorization("AdminOnly");

        // Kalender-Konfiguration anlegen / aktualisieren (Admin)
        app.MapPut("/admin/calendar/configs/{googleCalendarId}", async (
            string googleCalendarId,
            UpdateCalendarConfigDto dto,
            ICalendarConfigService configService) =>
        {
            var result = await configService.UpsertAsync(googleCalendarId, dto);
            return Results.Ok(result);
        }).RequireAuthorization("AdminOnly");

        // -------------------------------------------------------------------------
        // Calendar Event Endpoints
        // -------------------------------------------------------------------------

        // Nächste Veranstaltungen für das Dashboard
        app.MapGet("/calendar/upcoming-events", async (
            ICalendarService calendarService,
            ICalendarConfigService configService) =>
        {
            var now = DateTime.UtcNow;
            var configs = (await configService.GetAllAsync())
                .Where(c => string.Equals(c.CalendarType, "Veranstaltungen", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (configs.Count == 0)
                return Results.Ok(Array.Empty<object>());

            var configMap = configs.ToDictionary(c => c.GoogleCalendarId);
            var calendarIds = configs.Select(c => c.GoogleCalendarId);

            // Erst die nächsten 14 Tage laden
            var events = (await calendarService.GetEventsAsync(calendarIds, now, now.AddDays(14)))
                .OrderBy(e => e.Start ?? DateTime.MaxValue)
                .ToList();

            // Weniger als 2 Treffer → weiter in die Zukunft schauen und mindestens 2 nehmen
            if (events.Count < 2)
            {
                events = (await calendarService.GetEventsAsync(calendarIds, now, now.AddDays(365)))
                    .OrderBy(e => e.Start ?? DateTime.MaxValue)
                    .Take(2)
                    .ToList();
            }

            var enriched = events.Select(e =>
                configMap.TryGetValue(e.CalendarId, out var cfg)
                    ? e with { CalendarName = cfg.Name, CalendarColor = cfg.Color }
                    : e);

            return Results.Ok(enriched);
        }).RequireAuthorization("AnyRole");

        // Ereignisse für einen Zeitraum abrufen
        app.MapGet("/calendar/events", async (
            DateTime from,
            DateTime to,
            string? type,
            ICalendarService calendarService,
            ICalendarConfigService configService) =>
        {
            var all = await configService.GetAllAsync();
            // Wenn ein Typ angegeben: nach Typ filtern (unabhängig von IsVisible)
            // Ohne Typ: nur sichtbare Kalender (bestehende Hallenbelegung-Logik)
            var configs = (!string.IsNullOrWhiteSpace(type)
                ? all.Where(c => string.Equals(c.CalendarType, type, StringComparison.OrdinalIgnoreCase))
                : all.Where(c => c.IsVisible))
                .ToList();

            if (configs.Count == 0)
                return Results.Ok(Array.Empty<object>());

            var events = await calendarService.GetEventsAsync(
                configs.Select(c => c.GoogleCalendarId), from, to);

            // Ereignisse mit Kalender-Name und -Farbe anreichern
            var configMap = configs.ToDictionary(c => c.GoogleCalendarId);
            var enriched = events
                .Where(e => configMap.ContainsKey(e.CalendarId))
                .Select(e =>
                {
                    var cfg = configMap[e.CalendarId];
                    return e with { CalendarName = cfg.Name, CalendarColor = cfg.Color };
                })
                .ToList();

            return Results.Ok(enriched);
        }).RequireAuthorization("AnyRole");

        // Ereignis erstellen
        app.MapPost("/calendar/events", async (
            CreateCalendarEventDto dto,
            HttpContext ctx,
            ICalendarService calendarService,
            ICalendarConfigService configService,
            IUserService userService,
            IUserPreferencesService prefsService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var config = (await configService.GetAllAsync())
                .FirstOrDefault(c => c.GoogleCalendarId == dto.CalendarId);

            if (config is null)
                return Results.NotFound("Kalender nicht gefunden.");

            if (!HasWriteAccess(ctx, config))
                return Results.Forbid();

            var user = await userService.GetByIdAsync(parsedUserId);
            if (user is null)
                return Results.Unauthorized();

            // DisplayName aus den Nutzerpräferenzen verwenden, Fallback auf Google-Name
            var prefs = await prefsService.GetAsync(parsedUserId);
            var creatorName = !string.IsNullOrWhiteSpace(prefs.DisplayName)
                ? prefs.DisplayName
                : user.Name;

            var created = await calendarService.CreateEventAsync(
                dto.CalendarId, config.Name, config.Color, dto, creatorName, user.Email);

            return Results.Created($"/calendar/events/{dto.CalendarId}/{created.Id}", created);
        }).RequireAuthorization("AnyRole");

        // Ereignis aktualisieren
        app.MapPut("/calendar/events/{calendarId}/{eventId}", async (
            string calendarId,
            string eventId,
            UpdateCalendarEventDto dto,
            HttpContext ctx,
            ICalendarService calendarService,
            ICalendarConfigService configService) =>
        {
            var config = (await configService.GetAllAsync())
                .FirstOrDefault(c => c.GoogleCalendarId == calendarId);

            if (config is null)
                return Results.NotFound("Kalender nicht gefunden.");

            if (!HasWriteAccess(ctx, config))
                return Results.Forbid();

            var updated = await calendarService.UpdateEventAsync(
                calendarId, config.Name, config.Color, eventId, dto);

            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).RequireAuthorization("AnyRole");

        // Ereignis löschen
        app.MapDelete("/calendar/events/{calendarId}/{eventId}", async (
            string calendarId,
            string eventId,
            HttpContext ctx,
            ICalendarService calendarService,
            ICalendarConfigService configService) =>
        {
            var config = (await configService.GetAllAsync())
                .FirstOrDefault(c => c.GoogleCalendarId == calendarId);

            if (config is null)
                return Results.NotFound("Kalender nicht gefunden.");

            if (!HasWriteAccess(ctx, config))
                return Results.Forbid();

            await calendarService.DeleteEventAsync(calendarId, eventId);
            return Results.NoContent();
        }).RequireAuthorization("AnyRole");

        // -------------------------------------------------------------------------
        // Umfragen Endpoints
        // -------------------------------------------------------------------------

        app.MapGet("/umfrage", async (HttpContext ctx, IUmfrageService umfrageService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var isPrivileged = ctx.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Any(c => RoleGroups.Vorstand.Select(r => r.ToString()).Contains(c.Value)
                       || c.Value == Role.Admin.ToString());

            return Results.Ok(await umfrageService.GetAllAsync(parsedUserId, isPrivileged));
        }).RequireAuthorization("AnyRole");

        app.MapPost("/umfrage", async (
            CreateUmfrageDto dto,
            HttpContext ctx,
            IUmfrageService umfrageService,
            IPushNotificationService pushService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Question))
                return Results.BadRequest("Frage darf nicht leer sein.");

            if (dto.Options is null || dto.Options.Count < 2)
                return Results.BadRequest("Mindestens 2 Antwortmöglichkeiten sind erforderlich.");

            var created = await umfrageService.CreateAsync(parsedUserId, dto);

            // Notify users with Umfragen preference enabled
            var question = created.Question.Length > 70 ? created.Question[..67] + "…" : created.Question;
            _ = pushService.SendToUsersWithPreferenceAsync(
                p => p.NotifyUmfragen,
                "Neue Umfrage 📊",
                question,
                url: "/umfrage");

            return Results.Created($"/umfrage/{created.Id}", created);
        }).RequireAuthorization("AnyRole");

        app.MapPut("/umfrage/{id:guid}", async (
            Guid id,
            UpdateUmfrageDto dto,
            HttpContext ctx,
            IUmfrageService umfrageService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Question))
                return Results.BadRequest("Frage darf nicht leer sein.");

            if (dto.Options is null || dto.Options.Count < 2)
                return Results.BadRequest("Mindestens 2 Antwortmöglichkeiten sind erforderlich.");

            var isPrivileged = ctx.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Any(c => RoleGroups.Vorstand.Select(r => r.ToString()).Contains(c.Value)
                       || c.Value == Role.Admin.ToString());

            var (result, forbidden) = await umfrageService.UpdateAsync(id, parsedUserId, isPrivileged, dto);

            if (forbidden) return Results.Forbid();
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("AnyRole");

        app.MapDelete("/umfrage/{id:guid}", async (
            Guid id,
            HttpContext ctx,
            IUmfrageService umfrageService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var isPrivileged = ctx.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Any(c => RoleGroups.Vorstand.Select(r => r.ToString()).Contains(c.Value)
                       || c.Value == Role.Admin.ToString());

            var (success, forbidden) = await umfrageService.DeleteAsync(id, parsedUserId, isPrivileged);

            if (forbidden) return Results.Forbid();
            return success ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("AnyRole");

        app.MapPost("/umfrage/{id:guid}/vote", async (
            Guid id,
            VoteUmfrageDto dto,
            HttpContext ctx,
            IUmfrageService umfrageService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var isPrivileged = ctx.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Any(c => RoleGroups.Vorstand.Select(r => r.ToString()).Contains(c.Value)
                       || c.Value == Role.Admin.ToString());

            var (result, error) = await umfrageService.VoteAsync(id, parsedUserId, dto, isPrivileged);

            return error is not null
                ? Results.BadRequest(error)
                : Results.Ok(result);
        }).RequireAuthorization("AnyRole");

        app.MapPost("/umfrage/{id:guid}/close", async (
            Guid id,
            HttpContext ctx,
            IUmfrageService umfrageService) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var parsedUserId))
                return Results.Unauthorized();

            var isPrivileged = ctx.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Any(c => RoleGroups.Vorstand.Select(r => r.ToString()).Contains(c.Value)
                       || c.Value == Role.Admin.ToString());

            var (result, forbidden) = await umfrageService.CloseAsync(id, parsedUserId, isPrivileged);

            if (forbidden) return Results.Forbid();
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization("AnyRole");

        app.Run();
    }

    private static bool HasWriteAccess(HttpContext ctx, AkaKraft.Application.DTOs.CalendarConfigDto config)
    {
        var userRoles = ctx.User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToHashSet();

        // Admins und Chairman dürfen immer schreiben
        if (userRoles.Contains(Role.Admin.ToString()) ||
            userRoles.Contains(Role.Chairman.ToString()) ||
            userRoles.Contains(Role.ViceChairman.ToString()))
            return true;

        // Wenn keine spezifischen Rollen konfiguriert: nur Vorstand+Admin (bereits oben)
        var writeRoles = config.WriteRoles.ToList();
        if (writeRoles.Count == 0)
            return false;

        return writeRoles.Any(r => userRoles.Contains(r));
    }

    private static async Task EnsureMinioReadyAsync(WebApplication app)
    {
        var logger = app.Logger;

        using var scope = app.Services.CreateScope();
        var minio = scope.ServiceProvider.GetRequiredService<Minio.IMinioClient>();
        var opts  = scope.ServiceProvider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<AkaKraft.Infrastructure.Options.MinioOptions>>()
            .Value;

        var bucket = opts.BucketName;

        try
        {
            var exists = await minio.BucketExistsAsync(
                new Minio.DataModel.Args.BucketExistsArgs().WithBucket(bucket));

            if (!exists)
            {
                await minio.MakeBucketAsync(
                    new Minio.DataModel.Args.MakeBucketArgs().WithBucket(bucket));
                logger.LogInformation("MinIO-Bucket '{Bucket}' wurde erstellt.", bucket);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "MinIO-Bucket '{Bucket}' konnte nicht geprüft/erstellt werden. " +
                "Ist der MinIO-Server erreichbar und sind die Zugangsdaten korrekt?", bucket);
            return;
        }

        // Öffentliche Leseberechtigung setzen – schlägt auf manchen MinIO-Versionen
        // fehl, wenn SetBucketPolicy durch Server-Policy gesperrt ist.
        // Fallback: Bucket manuell in der MinIO-Console (http://localhost:9001) auf
        // "Anonymous access: readonly" setzen.
        try
        {
            var policy = $$"""
                {
                  "Version": "2012-10-17",
                  "Statement": [{
                    "Effect": "Allow",
                    "Principal": {"AWS": ["*"]},
                    "Action": ["s3:GetObject"],
                    "Resource": ["arn:aws:s3:::{{bucket}}/*"]
                  }]
                }
                """;

            await minio.SetPolicyAsync(
                new Minio.DataModel.Args.SetPolicyArgs()
                    .WithBucket(bucket)
                    .WithPolicy(policy));

            logger.LogInformation("MinIO-Bucket '{Bucket}': öffentlicher Lesezugriff gesetzt.", bucket);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "MinIO-Bucket '{Bucket}': Bucket-Policy konnte nicht gesetzt werden " +
                "(häufig bei neueren MinIO-Versionen). " +
                "Bitte Bucket manuell über die MinIO-Console (http://localhost:9001) " +
                "auf 'Anonymous access: readonly' stellen.", bucket);
        }
    }

    private static async Task EnsureAdminRoleAsync(WebApplication app)
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

        if (!user.Roles.Contains(AkaKraft.Domain.Enums.Role.Admin))
        {
            await userService.AssignRoleAsync(user.Id, AkaKraft.Domain.Enums.Role.Admin);
            logger.LogInformation("Admin-Rolle für '{Email}' vergeben.", adminEmail);
        }
    }
}
