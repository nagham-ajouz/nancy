using TripService.Application.Mapping;
using TripService.Application.Services;

namespace TripService.API.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddTripApplication(
        this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(TripMappingProfile).Assembly);

        services.AddScoped<DomainEventDispatcher>();
        services.AddScoped<TripAppService>();

        return services;
    }
}