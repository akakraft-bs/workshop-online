using AkaKraft.Application.Interfaces;
using AkaKraft.Infrastructure.Data;
using AkaKraft.Infrastructure.Options;
using AkaKraft.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using WebPush;


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

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IWerkzeugService, WerkzeugService>();
        services.AddScoped<IVerbrauchsmaterialService, VerbrauchsmaterialService>();
        services.AddScoped<IUploadService, UploadService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<ICalendarConfigService, CalendarConfigService>();
        services.AddSingleton<ICalendarService, GoogleCalendarService>();
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();

        // Push Notifications
        var vapidOpts = configuration.GetSection(VapidOptions.SectionName).Get<VapidOptions>()
            ?? new VapidOptions();

        // VAPID-Keys automatisch generieren falls nicht konfiguriert
        if (string.IsNullOrWhiteSpace(vapidOpts.PublicKey) || string.IsNullOrWhiteSpace(vapidOpts.PrivateKey))
        {
            var generated = VapidHelper.GenerateVapidKeys();
            vapidOpts.PublicKey = generated.PublicKey;
            vapidOpts.PrivateKey = generated.PrivateKey;
            if (string.IsNullOrWhiteSpace(vapidOpts.Subject))
                vapidOpts.Subject = "mailto:admin@akakraft.de";
        }

        services.Configure<VapidOptions>(opts =>
        {
            opts.Subject = vapidOpts.Subject;
            opts.PublicKey = vapidOpts.PublicKey;
            opts.PrivateKey = vapidOpts.PrivateKey;
        });

        services.AddScoped<IPushNotificationService, PushNotificationService>();
        services.AddScoped<INotificationPreferencesService, NotificationPreferencesService>();
        services.AddHostedService<WerkzeugRueckgabeReminderService>();

        return services;
    }
}
