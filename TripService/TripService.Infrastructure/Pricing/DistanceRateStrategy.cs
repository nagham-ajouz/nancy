using Microsoft.Extensions.Configuration;
using TripService.Domain.Pricing;
using Shared.ValueObjects;

namespace TripService.Infrastructure.Pricing;

public class DistanceRateStrategy : IPricingStrategy
{
    private readonly IConfiguration _configuration;

    public string Name => "DistanceRate";

    public DistanceRateStrategy(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Money Calculate(PricingContext context)
    {
        var ratePerKm = _configuration.GetValue<decimal>("Pricing:RatePerKm", 1.50m);
        var amount    = ratePerKm * context.DistanceKm;

        return new Money(Math.Round(amount, 2), "USD");
    }
}