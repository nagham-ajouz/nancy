namespace FleetService.Application.DTOs.Vehicle;

public class VehicleDto
{
    public Guid    Id          { get; set; }
    public string  PlateNumber { get; set; }
    public string  Model       { get; set; }
    public int     Year        { get; set; }
    public string  Type        { get; set; }
    public string  Status      { get; set; }
    public decimal Mileage     { get; set; }
    public Guid?   DriverId    { get; set; }
}