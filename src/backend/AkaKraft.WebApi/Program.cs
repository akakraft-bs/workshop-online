using System.Text.Json.Serialization;
using AkaKraft.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using AkaKraft.Infrastructure.Data;
using AkaKraft.WebApi.Services;
using Microsoft.EntityFrameworkCore;
using AkaKraft.WebApi.Endpoints;
using AkaKraft.WebApi.Helpers;

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

        builder
            .AddAkaKraftAuthentication()
            .AddAkaKraftAuthorization();
        
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
            options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
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
        await MinioHelper.EnsureMinioReadyAsync(app);

        // Sicherstellen dass der konfigurierte Admin die Admin-Rolle hat
        await AdminroleHelper.EnsureAdminRoleAsync(app);

        // Nginx leitet /api/... an das Backend weiter (ohne Prefix zu strippen).
        // UsePathBase entfernt /api aus dem Pfad und fügt es bei URL-Konstruktionen
        // (z. B. OAuth redirect_uri) automatisch wieder hinzu.
        app.UseForwardedHeaders();
        app.UsePathBase("/api");

        app.UseCors("Frontend");
        app.UseCookiePolicy();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapAuthEndpoints();
        app.MapUserEndpoints();
        app.MapUploadEndpoints();
        app.MapWerkzeugEndpoints();
        app.MapVerbrauchsmaterialEndpoints();
        app.MapMangelEndpoints();
        app.MapWunschEndpoints();
        app.MapUmfrageEndpoints();
        app.MapPushEndpoints();
        app.MapFeedbackEndpoints();
        app.MapCalendarEndpoints();
        app.MapHallenbuchEndpoints();
        app.MapVereinInfoEndpoints();
        app.MapDokumenteEndpoints();
        app.MapProjektEndpoints();
        app.MapVereinZugangEndpoints();
        app.MapAufgabeEndpoints();

        app.Run();
    }
}
