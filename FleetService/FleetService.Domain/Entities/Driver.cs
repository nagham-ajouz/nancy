using FleetService.Domain.Enums;
using FleetService.Domain.Events;
using FleetService.Domain.Exceptions;
using FleetService.Domain.ValueObjects;
using Shared.BaseClasses;
using Shared.Exceptions;

namespace FleetService.Domain.Entities;

public class Driver : AggregateRoot
{
    public string        FirstName     { get; private set; }
    public string        LastName      { get; private set; }
    public LicenseNumber LicenseNumber { get; private set; }
    public DateTime      LicenseExpiry { get; private set; }
    public DriverStatus  Status        { get; private set; }
    public Guid?         VehicleId     { get; private set; }
    
    private Driver()
    {
        FirstName     = null!;
        LastName      = null!;
        LicenseNumber = null!;
    }

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
    
    internal void AssignVehicle(Guid vehicleId)
    {
        VehicleId = vehicleId;
        Status    = DriverStatus.Available; 
    }

    internal void UnassignVehicle()
    {
        VehicleId = null;
    }
    
    public void MarkOnTrip()
    {
        if (Status != DriverStatus.Available)
            throw new DomainException($"Driver must be Available to start a trip. Current status: {Status}");

        Status = DriverStatus.OnTrip;
        AddDomainEvent(new DriverStatusChangedEvent(Id, Status));
    }

    public void MarkAvailable()
    {
        Status = DriverStatus.Available;
        AddDomainEvent(new DriverStatusChangedEvent(Id, Status));
    }

    public void Deactivate()
    {
        if (Status == DriverStatus.OnTrip)
            throw new DomainException("Cannot deactivate a driver who is currently on a trip.");

        Status = DriverStatus.Inactive;
        
        AddDomainEvent(new DriverStatusChangedEvent(Id, Status));
    }
    
    public void UpdateDetails(string firstName, string lastName, DateTime licenseExpiry)
    {
        if (string.IsNullOrWhiteSpace(firstName)) 
            throw new ArgumentException("First name required.");
        if (string.IsNullOrWhiteSpace(lastName))  
            throw new ArgumentException("Last name required.");
        FirstName     = firstName;
        LastName      = lastName;
        LicenseExpiry = licenseExpiry;
    }
    

}