namespace FleetService.Application.DTOs.Driver;

public class CreateDriverDto
{
    public string   FirstName     { get; set; } = null!;
    public string   LastName      { get; set; } = null!;
    public string   LicenseNumber { get; set; } = null!;
    public DateTime LicenseExpiry { get; set; }
    public string?  KeycloakUserId { get; set; }
}