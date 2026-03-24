namespace TripService.Domain.Pricing;

public class PricingContext
{
    // base rate
    public string   VehicleType  { get; init; } = null!;

    // Haversine calculation
    public decimal  DistanceKm   { get; init; }

    // used for peak hour detection
    public DateTime StartTime    { get; init; }
    
    public DateTime EndTime      { get; init; }
    
    public double DurationMinutes => (EndTime - StartTime).TotalMinutes;
}