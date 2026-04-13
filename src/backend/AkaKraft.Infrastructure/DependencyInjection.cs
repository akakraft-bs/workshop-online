using AkaKraft.Application.Interfaces;
using AkaKraft.Infrastructure.Data;
using AkaKraft.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AkaKraft.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IWerkzeugService, WerkzeugService>();
        services.AddScoped<IVerbrauchsmaterialService, VerbrauchsmaterialService>();
        services.AddScoped<IUploadService, UploadService>();

        return services;
    }
}
