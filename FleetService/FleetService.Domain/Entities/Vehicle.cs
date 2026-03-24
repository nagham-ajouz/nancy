using FleetService.Domain.Enums;
using FleetService.Domain.Events;
using FleetService.Domain.Exceptions;
using FleetService.Domain.ValueObjects;
using Shared.BaseClasses;
using Shared.Exceptions;

namespace FleetService.Domain.Entities;

public class Vehicle : AggregateRoot
{
    public PlateNumber  PlateNumber { get; private set; }
    public string       Model       { get; private set; }
    public int          Year        { get; private set; }
    public VehicleType  Type        { get; private set; }
    public VehicleStatus Status     { get; private set; }
    public decimal      Mileage     { get; private set; }
    public Guid?        DriverId    { get; private set; }
    
    private Vehicle()
    {
        PlateNumber = null!;
        Model       = null!;
    }

    public Vehicle(Guid id, PlateNumber plateNumber, string model, int year, VehicleType type)
    {
        if (string.IsNullOrWhiteSpace(model)) 
            throw new ArgumentException("Model required.");
        if (year < 1886 || year > DateTime.UtcNow.Year + 1) 
            throw new ArgumentException("Invalid year.");

        Id          = id;
        PlateNumber = plateNumber;
        Model       = model;
        Year        = year;
        Type        = type;
        Status      = VehicleStatus.Registered;
        Mileage     = 0;
    }
    
    public void Activate()
    {
        if (Status != VehicleStatus.Registered)
            throw new InvalidStateTransitionException(Status, VehicleStatus.Active);

        Status = VehicleStatus.Active;
        AddDomainEvent(new VehicleStatusChangedEvent(Id, Status, Type));
    }

    public void SendToMaintenance()
    {
        if (Status != VehicleStatus.Active)
            throw new InvalidStateTransitionException(Status, VehicleStatus.InMaintenance);

        Status = VehicleStatus.InMaintenance;
        AddDomainEvent(new VehicleStatusChangedEvent(Id, Status, Type));
    }

    public void CompleteMaintenance()
    {
        if (Status != VehicleStatus.InMaintenance)
            throw new InvalidStateTransitionException(Status, VehicleStatus.Active);

        Status = VehicleStatus.Active;
        AddDomainEvent(new VehicleStatusChangedEvent(Id, Status, Type));
    }

    public void Decommission()
    {
        if (Status != VehicleStatus.Active && Status != VehicleStatus.InMaintenance)
            throw new InvalidStateTransitionException(Status, VehicleStatus.Decommissioned);

        // Decommission guard: caller must ensure no active trip exists before calling this
        if (DriverId.HasValue)
            throw new DomainException("Cannot decommission a vehicle with an assigned driver.");

        Status = VehicleStatus.Decommissioned;
        AddDomainEvent(new VehicleStatusChangedEvent(Id, Status, Type));
    }
    
    public void AssignDriver(Driver driver)
    {
        if (Status != VehicleStatus.Active)
            throw new DomainException("Vehicle must be Active to assign a driver.");

        if (DriverId.HasValue)
            throw new DomainException("Vehicle already has an assigned driver.");

        if (driver.VehicleId.HasValue)
            throw new DomainException("Driver is already assigned to another vehicle.");

        if (driver.LicenseExpiry <= DateTime.UtcNow)
            throw new LicenseExpiredException(driver.Id);

        if (driver.Status != DriverStatus.Available)
            throw new DriverNotAvailableException(driver.Id);

        DriverId = driver.Id;
        driver.AssignVehicle(Id);

        AddDomainEvent(new DriverAssignedEvent(Id, driver.Id));
    }

    public void UnassignDriver(Driver driver)
    {
        if (DriverId != driver.Id)
            throw new DomainException("This driver is not assigned to this vehicle.");

        DriverId = null;
        driver.UnassignVehicle();

        AddDomainEvent(new DriverUnassignedEvent(Id, driver.Id));
    }
    
    public void AddMileage(decimal km)
    {
        if (km < 0) 
            throw new ArgumentException("Mileage cannot be negative.");
        Mileage += km;
    }
    
    // Called by application service on PUT — only safe fields can be updated directly
    public void UpdateDetails(string model, int year)
    {
        if (string.IsNullOrWhiteSpace(model)) 
            throw new ArgumentException("Model required.");
        if (year < 1886 || year > DateTime.UtcNow.Year + 1) 
            throw new ArgumentException("Invalid year.");
        Model = model;
        Year  = year;
    }
}