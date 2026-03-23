using FleetService.Infrastructure.Cache;
using FleetService.Infrastructure.Messaging.Consumers;
using FleetService.Infrastructure.Messaging.Publishers;
using FleetService.Infrastructure.Persistence;
using FleetService.Infrastructure.Repositories;
using FleetService.Application.Interfaces;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FleetService.API.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddFleetInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<FleetDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        // Redis
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName  = "FleetManagement:";
        });

        // Repositories
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IDriverRepository,  DriverRepository>();

        // Cache service
        services.AddScoped<IFleetCacheService, FleetCacheService>();

        // Publisher
        services.AddScoped<IFleetEventPublisher, FleetEventPublisher>();

        // RabbitMQ
        services.AddMassTransit(x =>
        {
            x.AddConsumer<TripStartedConsumer>();
            x.AddConsumer<TripCompletedConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMQ:Host"], "/", h =>
                {
                    h.Username(configuration["RabbitMQ:Username"]!);
                    h.Password(configuration["RabbitMQ:Password"]!);
                });

                cfg.ReceiveEndpoint("fleet-trip-started", e =>
                    e.ConfigureConsumer<TripStartedConsumer>(ctx));

                cfg.ReceiveEndpoint("fleet-trip-completed", e =>
                    e.ConfigureConsumer<TripCompletedConsumer>(ctx));
            });
        });

        return services;
    }
}