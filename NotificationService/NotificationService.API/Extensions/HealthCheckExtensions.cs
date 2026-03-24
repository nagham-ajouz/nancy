using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace NotificationService.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddNotificationHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(
                configuration.GetConnectionString("Default")!,
                name: "postgresql",
                failureStatus: HealthStatus.Unhealthy)
            .AddRabbitMQ(
                rabbitConnectionString: $"amqp://{configuration["RabbitMQ:Username"]}:{configuration["RabbitMQ:Password"]}@{configuration["RabbitMQ:Host"]}",
                name: "rabbitmq",
                failureStatus: HealthStatus.Degraded);

        return services;
    }

    public static HealthCheckOptions GetHealthResponse(string serviceName) =>
        new()
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new
                {
                    service = serviceName,
                    status  = report.Status.ToString(),
                    checks  = report.Entries.Select(e => new
                    {
                        name    = e.Key,
                        status  = e.Value.Status.ToString(),
                        description = e.Value.Description
                    })
                });
                await context.Response.WriteAsync(result);
            }
        };
}