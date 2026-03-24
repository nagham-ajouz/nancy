using TripService.Application.Mapping;
using TripService.Application.Services;
using TripService.Domain.Pricing;
using TripService.Infrastructure.Pricing;

namespace TripService.API.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddTripApplication(
        this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(TripMappingProfile).Assembly);
        
        services.AddScoped<IPricingStrategy, BaseRateStrategy>();
        services.AddScoped<IPricingStrategy, DistanceRateStrategy>();
        services.AddScoped<IPricingStrategy, PeakHourSurchargeStrategy>();
        
        services.AddScoped<TripPricingCalculator>();
        
        services.AddScoped<DomainEventDispatcher>();
        services.AddScoped<TripAppService>();

        return services;
    }
}