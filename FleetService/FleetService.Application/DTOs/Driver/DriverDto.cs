namespace FleetService.Application.DTOs.Driver;

public class DriverDto
{
    public Guid     Id            { get; set; }
    public string   FirstName     { get; set; } = null!;
    public string   LastName      { get; set; } = null!;
    public string   LicenseNumber { get; set; } = null!;
    public DateTime LicenseExpiry { get; set; }
    public string   Status        { get; set; } = null!;
    public Guid?    VehicleId     { get; set; }
}