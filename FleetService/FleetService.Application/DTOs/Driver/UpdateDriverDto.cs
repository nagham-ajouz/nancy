namespace FleetService.Application.DTOs.Driver;

public class UpdateDriverDto
{
    public string   FirstName     { get; set; } = null!;
    public string   LastName      { get; set; } = null!;
    public DateTime LicenseExpiry { get; set; }
}