using Microsoft.Extensions.Configuration;
using TripService.Domain.Pricing;
using Shared.ValueObjects;

namespace TripService.Infrastructure.Pricing;

// Base rate varies by vehicle type
// Rates are configurable via appsettings.json — not hardcoded
public class BaseRateStrategy : IPricingStrategy
{
    private readonly IConfiguration _configuration;

    public string Name => "BaseRate";

    public BaseRateStrategy(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Money Calculate(PricingContext context)
    {
        // Read rates from configuration — injectable, not hardcoded
        var rate = context.VehicleType switch
        {
            "Sedan" => _configuration.GetValue<decimal>("Pricing:BaseRates:Sedan", 5.00m),
            "SUV"   => _configuration.GetValue<decimal>("Pricing:BaseRates:SUV",   7.00m),
            "Van"   => _configuration.GetValue<decimal>("Pricing:BaseRates:Van",   8.00m),
            "Truck" => _configuration.GetValue<decimal>("Pricing:BaseRates:Truck", 10.00m),
            _       => _configuration.GetValue<decimal>("Pricing:BaseRates:Default", 5.00m)
        };

        return new Money(rate, "USD");
    }
}