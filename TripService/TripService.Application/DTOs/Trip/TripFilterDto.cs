namespace TripService.Application.DTOs.Trip;

public class TripFilterDto
{
    public Guid?       DriverId  { get; set; }
    public Guid?       VehicleId { get; set; }
    public string?     Status    { get; set; }
    public DateTime?   From      { get; set; }
    public DateTime?   To        { get; set; }
}