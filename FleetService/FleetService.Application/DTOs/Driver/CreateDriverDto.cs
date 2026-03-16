namespace FleetService.Application.DTOs.Driver;

public class CreateDriverDto
{
    public string   FirstName     { get; set; }
    public string   LastName      { get; set; }
    public string   LicenseNumber { get; set; }
    public DateTime LicenseExpiry { get; set; }
}