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
}