using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace NotificationService.API.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddNotificationAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority            = configuration["Keycloak:Authority"];
                options.Audience             = configuration["Keycloak:Audience"];
                options.RequireHttpsMetadata = false; // dev/docker only
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    RoleClaimType            = "realm_access.roles" // Keycloak role claim path
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOrFleetManager", policy =>
                policy.RequireRole("Admin", "FleetManager"));

            options.AddPolicy("DriverOnly", policy =>
                policy.RequireRole("Driver"));

            options.AddPolicy("AnyRole", policy =>
                policy.RequireAuthenticatedUser());
        });

        return services;
    }
}