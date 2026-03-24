using Shared.ValueObjects;

namespace TripService.Domain.Pricing;

// Every pricing strategy must implement this
// The strategy receives all trip context and returns the calculated cost
public interface IPricingStrategy
{
    // Name shown in logs so you know which strategy was used
    string Name { get; }

    Money Calculate(PricingContext context);
}