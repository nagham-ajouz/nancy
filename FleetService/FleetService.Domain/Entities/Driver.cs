using FleetService.Domain.Enums;
using FleetService.Domain.ValueObjects;
using Shared.BaseClasses;

namespace FleetService.Domain.Entities;

public class Driver : AggregateRoot
{
    public string        FirstName     { get; private set; }
    public string        LastName      { get; private set; }
    public LicenseNumber LicenseNumber { get; private set; }
    public DateTime      LicenseExpiry { get; private set; }
    public DriverStatus  Status        { get; private set; }
    public Guid?         VehicleId     { get; private set; }
    
    private Driver() { }

    public Driver(Guid id, string firstName, string lastName,
        LicenseNumber licenseNumber, DateTime licenseExpiry)
    {
        if (string.IsNullOrWhiteSpace(firstName)) 
            throw new ArgumentException("First name required.");
        if (string.IsNullOrWhiteSpace(lastName))  
            throw new ArgumentException("Last name required.");

        Id            = id;
        FirstName     = firstName;
        LastName      = lastName;
        LicenseNumber = licenseNumber;
        LicenseExpiry = licenseExpiry;
        Status        = DriverStatus.Available;
    }
}