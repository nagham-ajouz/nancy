using MassTransit;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Services;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Messaging.Consumers;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Repositories;

namespace NotificationService.API.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        // Repository + Application service
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationService, NotificationAppService>();

        // MassTransit (RabbitMQ) — fan-out: own queues so Fleet/Trip queues are not affected
        services.AddMassTransit(x =>
        {
            x.AddConsumer<TripCompletedConsumer>();
            x.AddConsumer<VehicleStatusChangedConsumer>();
            x.AddConsumer<DriverStatusChangedConsumer>();
            x.AddConsumer<DriverLicenseExpiryConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMQ:Host"], "/", h =>
                {
                    h.Username(configuration["RabbitMQ:Username"]!);
                    h.Password(configuration["RabbitMQ:Password"]!);
                });

                // Separate queues — won't interfere with fleet-trip-completed or trip-vehicle-status-changed
                cfg.ReceiveEndpoint("notification-trip-completed", e =>
                    e.ConfigureConsumer<TripCompletedConsumer>(ctx));

                cfg.ReceiveEndpoint("notification-vehicle-status-changed", e =>
                    e.ConfigureConsumer<VehicleStatusChangedConsumer>(ctx));

                cfg.ReceiveEndpoint("notification-driver-status-changed", e =>
                    e.ConfigureConsumer<DriverStatusChangedConsumer>(ctx));

                cfg.ReceiveEndpoint("notification-driver-license-expiry", e =>
                    e.ConfigureConsumer<DriverLicenseExpiryConsumer>(ctx));
            });
        });

        return services;
    }
}