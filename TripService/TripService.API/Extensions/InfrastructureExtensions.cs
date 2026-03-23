using MassTransit;
using Microsoft.EntityFrameworkCore;
using TripService.Application.Interfaces;
using TripService.Infrastructure.Cache;
using TripService.Infrastructure.Messaging.Consumers;
using TripService.Infrastructure.Messaging.Publishers;
using TripService.Infrastructure.Persistence;
using TripService.Infrastructure.Repositories;

namespace TripService.API.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddTripInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<TripDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        // Redis
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "FleetManagement:";
        });

        // SignalR
        services.AddSignalR();

        // Repositories
        services.AddScoped<ITripRepository, TripRepository>();

        // Cache
        services.AddScoped<IVehicleAvailabilityCache, VehicleAvailabilityCache>();

        // Messaging Publisher
        services.AddScoped<ITripEventPublisher, TripEventPublisher>();

        // MassTransit (RabbitMQ)
        services.AddMassTransit(x =>
        {
            x.AddConsumer<VehicleStatusChangedConsumer>();
            x.AddConsumer<DriverStatusChangedConsumer>();
            x.AddConsumer<DriverAssignedConsumer>();
            x.AddConsumer<DriverUnassignedConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                var host = configuration["RabbitMQ:Host"];
                var username = configuration["RabbitMQ:Username"];
                var password = configuration["RabbitMQ:Password"];

                cfg.Host(host, "/", h =>
                {
                    h.Username(username!);
                    h.Password(password!);
                });

                cfg.ReceiveEndpoint("trip-vehicle-status-changed", e =>
                    e.ConfigureConsumer<VehicleStatusChangedConsumer>(ctx));

                cfg.ReceiveEndpoint("trip-driver-status-changed", e =>
                    e.ConfigureConsumer<DriverStatusChangedConsumer>(ctx));

                cfg.ReceiveEndpoint("trip-driver-assigned", e =>
                    e.ConfigureConsumer<DriverAssignedConsumer>(ctx));

                cfg.ReceiveEndpoint("trip-driver-unassigned", e =>
                    e.ConfigureConsumer<DriverUnassignedConsumer>(ctx));
            });
        });

        return services;
    }
}