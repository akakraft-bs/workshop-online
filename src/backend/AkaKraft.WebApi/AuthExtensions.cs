using System.Security.Claims;
using System.Text;
using AkaKraft.Domain.Enums;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace AkaKraft.WebApi;

public static class AuthExtensions
{
    public static IServiceCollection AddAkaKraftAuthentication(this WebApplicationBuilder builder)
    {
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
        return builder.Services;
    }

    public static IServiceCollection AddAkaKraftAuthorization(this IServiceCollection services)
    {
        // -------------------------------------------------------------------------
        // Authorization Policies
        // -------------------------------------------------------------------------
        services.AddAuthorization(options =>
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

        return services;
    }
}