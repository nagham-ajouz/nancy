using Microsoft.Extensions.Configuration;
using TripService.Domain.Pricing;
using Shared.ValueObjects;

namespace TripService.Infrastructure.Pricing;

// Adds a surcharge for trips during peak hours
// Peak hours: 7-9 AM and 5-7 PM on weekdays
public class PeakHourSurchargeStrategy : IPricingStrategy
{
    private readonly IConfiguration _configuration;

    public string Name => "PeakHourSurcharge";

    public PeakHourSurchargeStrategy(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Money Calculate(PricingContext context)
    {
        if (!IsPeakHour(context.StartTime))
            return new Money(0, "USD");

        var surcharge = _configuration.GetValue<decimal>("Pricing:PeakHourSurcharge", 3.00m);
        return new Money(surcharge, "USD");
    }

    private bool IsPeakHour(DateTime startTime)
    {
        // No surcharge on weekends
        if (startTime.DayOfWeek == DayOfWeek.Saturday ||
            startTime.DayOfWeek == DayOfWeek.Sunday)
            return false;

        var hour = startTime.Hour;

        // Morning peak: 7-9 AM
        // Evening peak: 5-7 PM
        return (hour >= 7 && hour < 9) || (hour >= 17 && hour < 19);
    }
}