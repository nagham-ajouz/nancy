namespace FleetService.Application.DTOs.Vehicle;

public class CreateVehicleDto
{
    public string PlateNumber { get; set; } = null!;
    public string Model       { get; set; } = null!;
    public int    Year        { get; set; }
    public string Type        { get; set; } = null!;
}