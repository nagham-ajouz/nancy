using FleetService.Domain.Entities;
using FleetService.Domain.Enums;
using FleetService.Domain.Exceptions;
using FleetService.Domain.ValueObjects;
using FluentAssertions;
using Shared.Exceptions;
using Xunit;

namespace FleetService.Tests.Domain;

public class VehicleAssignmentTests
{
    private static Vehicle CreateActiveVehicle()
    {
        var v = new Vehicle(Guid.NewGuid(), new PlateNumber("ABC-1234"),
                            "Toyota", 2022, VehicleType.Sedan);
        v.Activate();
        return v;
    }

    private static Driver CreateAvailableDriver(DateTime? expiry = null)
    {
        return new Driver(Guid.NewGuid(), "John", "Doe",
            new LicenseNumber("LB123456"),
            expiry ?? DateTime.UtcNow.AddYears(2));
    }

    [Fact]
    public void AssignDriver_WhenVehicleActiveAndDriverAvailable_ShouldAssign()
    {
        var vehicle = CreateActiveVehicle();
        var driver  = CreateAvailableDriver();

        vehicle.AssignDriver(driver);

        vehicle.DriverId.Should().Be(driver.Id);
        driver.VehicleId.Should().Be(vehicle.Id);
    }

    [Fact]
    public void AssignDriver_WhenLicenseExpired_ShouldThrow()
    {
        var vehicle = CreateActiveVehicle();
        // License expired yesterday
        var driver  = CreateAvailableDriver(DateTime.UtcNow.AddDays(-1));

        Action act = () => vehicle.AssignDriver(driver);

        act.Should().Throw<LicenseExpiredException>();
    }

    [Fact]
    public void AssignDriver_WhenVehicleNotActive_ShouldThrow()
    {
        var vehicle = new Vehicle(Guid.NewGuid(), new PlateNumber("ABC-1234"),
                                  "Toyota", 2022, VehicleType.Sedan);
        // Vehicle is still Registered — not Active
        var driver = CreateAvailableDriver();

        Action act = () => vehicle.AssignDriver(driver);

        act.Should().Throw<DomainException>()
           .WithMessage("*Active*");
    }

    [Fact]
    public void AssignDriver_WhenDriverAlreadyAssigned_ShouldThrow()
    {
        var vehicle1 = CreateActiveVehicle();
        var vehicle2 = new Vehicle(Guid.NewGuid(), new PlateNumber("DEF-5678"),
                                   "Honda", 2021, VehicleType.SUV);
        vehicle2.Activate();
        var driver = CreateAvailableDriver();

        vehicle1.AssignDriver(driver);

        // Try to assign same driver to second vehicle
        Action act = () => vehicle2.AssignDriver(driver);

        act.Should().Throw<DomainException>()
           .WithMessage("*already assigned*");
    }

    [Fact]
    public void UnassignDriver_WhenAssigned_ShouldClearBothSides()
    {
        var vehicle = CreateActiveVehicle();
        var driver  = CreateAvailableDriver();
        vehicle.AssignDriver(driver);

        vehicle.UnassignDriver(driver);

        vehicle.DriverId.Should().BeNull();
        driver.VehicleId.Should().BeNull();
    }

    [Fact]
    public void AddMileage_ShouldIncreaseMileage()
    {
        var vehicle = CreateActiveVehicle();

        vehicle.AddMileage(100);
        vehicle.AddMileage(50);

        vehicle.Mileage.Should().Be(150);
    }

    [Fact]
    public void AddMileage_WhenNegative_ShouldThrow()
    {
        var vehicle = CreateActiveVehicle();

        Action act = () => vehicle.AddMileage(-10);

        act.Should().Throw<ArgumentException>();
    }
}