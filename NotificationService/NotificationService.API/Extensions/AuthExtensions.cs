using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

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
                options.RequireHttpsMetadata = false;
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer   = true,
                    ValidIssuers = new[]
                    {
                        configuration["Keycloak:Authority"],
                        "http://localhost:8080/realms/fleet-management"
                    }
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
                        if (claimsIdentity == null) return Task.CompletedTask;

                        // Extract roles from Keycloak's nested structure
                        var realmAccessClaim = context.Principal?.FindFirst("realm_access");
                        if (realmAccessClaim == null) return Task.CompletedTask;

                        var realmAccess = System.Text.Json.JsonDocument.Parse(realmAccessClaim.Value);
                        if (realmAccess.RootElement.TryGetProperty("roles", out var rolesElement))
                        {
                            foreach (var role in rolesElement.EnumerateArray())
                            {
                                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role.GetString()!));
                            }
                        }

                        return Task.CompletedTask;
                    }
                };
            });
        services.AddAuthorization();

        return services;
    }
}