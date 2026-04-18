using AkaKraft.Application.Interfaces;
using AkaKraft.Infrastructure.Data;
using AkaKraft.Infrastructure.Options;
using AkaKraft.Infrastructure.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            var firebaseApp = FirebaseApp.DefaultInstance ?? FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(firebaseJson),
            });
            services.AddSingleton(firebaseApp);
            services.AddScoped<IPushNotificationService, FcmPushNotificationService>();
        }
        else
        {
            services.AddScoped<IPushNotificationService, NoOpPushNotificationService>();
        }

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

        return services;
    }
}
