using AkaKraft.Application.Interfaces;
using AkaKraft.Infrastructure.Data;
using AkaKraft.Infrastructure.Options;
using AkaKraft.Infrastructure.Services;
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

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IWerkzeugService, WerkzeugService>();
        services.AddScoped<IVerbrauchsmaterialService, VerbrauchsmaterialService>();
        services.AddScoped<IUploadService, UploadService>();
        services.AddScoped<IFeedbackService, FeedbackService>();

        return services;
    }
}
