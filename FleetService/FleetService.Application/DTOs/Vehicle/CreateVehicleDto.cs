namespace FleetService.Application.DTOs.Vehicle;

public class CreateVehicleDto
{
    public string PlateNumber { get; set; }
    public string Model       { get; set; }
    public int    Year        { get; set; }
    public string Type        { get; set; }
}