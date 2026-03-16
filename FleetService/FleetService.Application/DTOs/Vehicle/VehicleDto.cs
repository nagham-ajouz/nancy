namespace FleetService.Application.DTOs.Vehicle;

public class VehicleDto
{
    public Guid    Id          { get; set; }
    public string  PlateNumber { get; set; } = null!;
    public string  Model       { get; set; } = null!;
    public int     Year        { get; set; }
    public string  Type        { get; set; } = null!;
    public string  Status      { get; set; } = null!;
    public decimal Mileage     { get; set; }
    public Guid?   DriverId    { get; set; }
}