namespace FleetService.Application.DTOs.Driver;

public class DriverDto
{
    public Guid     Id            { get; set; }
    public string   FirstName     { get; set; }
    public string   LastName      { get; set; }
    public string   LicenseNumber { get; set; }
    public DateTime LicenseExpiry { get; set; }
    public string   Status        { get; set; }
    public Guid?    VehicleId     { get; set; }
}