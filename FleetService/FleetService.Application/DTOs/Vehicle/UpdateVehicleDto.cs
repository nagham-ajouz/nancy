namespace FleetService.Application.DTOs.Vehicle;

public class UpdateVehicleDto
{
    public string Model { get; set; } = null!;
    public int    Year  { get; set; }
}