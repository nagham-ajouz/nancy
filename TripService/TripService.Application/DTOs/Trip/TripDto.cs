namespace TripService.Application.DTOs.Trip;

public class TripDto
{
    public Guid      Id            { get; set; }
    public Guid      VehicleId     { get; set; }
    public Guid      DriverId      { get; set; }
    public string    Status        { get; set; } = null!;
    public LocationDto StartLocation { get; set; } = null!;
    public LocationDto EndLocation   { get; set; } = null!;
    public DateTime? StartTime     { get; set; }
    public DateTime? EndTime       { get; set; }
    public decimal?  DistanceKm    { get; set; }
    public decimal?  CostAmount    { get; set; }
    public string?   CostCurrency  { get; set; }
}