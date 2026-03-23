using FleetService.Application.Interfaces;
using FleetService.Application.Mapping;
using FleetService.Application.Services;

namespace FleetService.API.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddFleetApplication(
        this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(FleetMappingProfile).Assembly);
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<DriverService>();

        return services;
    }
}