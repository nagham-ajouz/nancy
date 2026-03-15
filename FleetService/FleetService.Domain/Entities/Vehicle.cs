using FleetService.Domain.Enums;
using FleetService.Domain.ValueObjects;
using Shared.BaseClasses;

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
    
    private Vehicle() { }

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
}