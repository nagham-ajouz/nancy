using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace FleetService.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddFleetHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(
                connectionString: configuration.GetConnectionString("Default")!,
                name: "postgresql",
                tags: new[] { "database" })
            .AddRabbitMQ(
                rabbitConnectionString: $"amqp://{configuration["RabbitMQ:Username"]}:{configuration["RabbitMQ:Password"]}@{configuration["RabbitMQ:Host"]}",
                name: "rabbitmq",
                tags: new[] { "messaging" })
            .AddRedis(
                redisConnectionString: configuration.GetConnectionString("Redis")!,
                name: "redis",
                tags: new[] { "cache" });

        return services;
    }
    public static HealthCheckOptions GetHealthResponse(string serviceName)
    {
        return new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";

                var result = new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow,
                    service = serviceName,
                    checks = report.Entries.ToDictionary(
                        e => e.Key,
                        e => new
                        {
                            status = e.Value.Status.ToString(),
                            description = e.Value.Description,
                            duration = e.Value.Duration.TotalMilliseconds + "ms"
                        })
                };

                await context.Response.WriteAsJsonAsync(result);
            }
        };
    }
}