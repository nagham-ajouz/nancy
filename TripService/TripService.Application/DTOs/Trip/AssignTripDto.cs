namespace TripService.Application.DTOs.Trip;

public class AssignTripDto
{
    public Guid VehicleId { get; set; }
    public Guid DriverId  { get; set; }
}