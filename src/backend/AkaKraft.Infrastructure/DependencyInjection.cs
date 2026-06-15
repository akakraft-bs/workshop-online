using AkaKraft.Application.Interfaces;
using AkaKraft.Infrastructure.Data;
using AkaKraft.Infrastructure.Options;
using AkaKraft.Infrastructure.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;


namespace AkaKraft.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // MinIO
        var minioOpts = configuration.GetSection(MinioOptions.SectionName).Get<MinioOptions>()
            ?? throw new InvalidOperationException("MinIO-Konfiguration fehlt (Abschnitt 'Minio').");

        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));

        services.AddMinio(client => client
            .WithEndpoint(minioOpts.Endpoint)
            .WithCredentials(minioOpts.AccessKey, minioOpts.SecretKey)
            .WithSSL(minioOpts.UseSSL)
            .Build());

        // Firebase Admin SDK (optional – Push-Notifications werden nur gesendet wenn konfiguriert)
        var firebaseJson = configuration["Firebase:AdminSdkJson"];
        if (!string.IsNullOrWhiteSpace(firebaseJson))
        {
            using var firebaseDoc = System.Text.Json.JsonDocument.Parse(firebaseJson);
            var root = firebaseDoc.RootElement;
            var projectId   = root.TryGetProperty("project_id",  out var pidEl) ? pidEl.GetString()! : null;
            var clientEmail = root.TryGetProperty("client_email", out var ceEl)  ? ceEl.GetString()! : throw new InvalidOperationException("Firebase JSON: client_email fehlt.");
            var privateKey  = root.TryGetProperty("private_key",  out var pkEl)  ? pkEl.GetString()! : throw new InvalidOperationException("Firebase JSON: private_key fehlt.");

            var saCredential = new ServiceAccountCredential(
                new ServiceAccountCredential.Initializer(clientEmail)
                {
                    ProjectId = projectId,
                }.FromPrivateKey(privateKey));
            var credential = saCredential.ToGoogleCredential();
            var firebaseApp = FirebaseApp.DefaultInstance ?? FirebaseApp.Create(new AppOptions
            {
                Credential = credential,
                ProjectId = projectId,
            });
            services.AddSingleton(firebaseApp);
            services.AddScoped<IPushNotificationService, FcmPushNotificationService>();
        }
        else
        {
            services.AddScoped<IPushNotificationService, NoOpPushNotificationService>();
        }

        // E-Mail
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IWerkzeugService, WerkzeugService>();
        services.AddScoped<IVerbrauchsmaterialService, VerbrauchsmaterialService>();
        services.AddScoped<IUploadService, UploadService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<ICalendarConfigService, CalendarConfigService>();
        services.AddSingleton<ICalendarService, GoogleCalendarService>();
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<IMangelService, MangelService>();
        services.AddScoped<IWunschService, WunschService>();
        services.AddScoped<IUmfrageService, UmfrageService>();
        services.AddScoped<IHallenbuchService, HallenbuchService>();
        services.AddScoped<IVereinInfoService, VereinInfoService>();
        services.AddScoped<IDokumenteService, DokumenteService>();
        services.AddScoped<IProjektService, ProjektService>();
        services.AddScoped<IVereinZugangService, VereinZugangService>();
        services.AddScoped<IAufgabeService, AufgabeService>();

        return services;
    }
}
