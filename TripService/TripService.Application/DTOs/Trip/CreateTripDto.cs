namespace TripService.Application.DTOs.Trip;

public class CreateTripDto
{
    public LocationDto StartLocation { get; set; } = null!;
    public LocationDto EndLocation   { get; set; } = null!;
}