using FleetService.Domain.Entities;
using FleetService.Domain.Enums;
using FleetService.Domain.Exceptions;
using FleetService.Domain.ValueObjects;
using FluentAssertions;
using Shared.Exceptions;
using Xunit;

namespace FleetService.Tests.Domain;

public class VehicleStateTransitionTests
{
    // Helper — creates a valid vehicle in Registered state
    private static Vehicle CreateVehicle()
    {
        return new Vehicle(
            Guid.NewGuid(),
            new PlateNumber("ABC-1234"),
            "Toyota Camry",
            2022,
            VehicleType.Sedan);
    }

    // ── Activate ─────────────────────────────────────────────────────────────

    [Fact]
    public void Activate_WhenRegistered_ShouldBecomeActive()
    {
        var vehicle = CreateVehicle();

        vehicle.Activate();

        vehicle.Status.Should().Be(VehicleStatus.Active);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldThrow()
    {
        var vehicle = CreateVehicle();
        vehicle.Activate();

        // Cannot activate an already active vehicle
        Action act = () => vehicle.Activate();

        act.Should().Throw<InvalidStateTransitionException>()
           .WithMessage("*Active*");
    }

    [Fact]
    public void Activate_WhenDecommissioned_ShouldThrow()
    {
        var vehicle = CreateVehicle();
        vehicle.Activate();
        vehicle.Decommission();

        Action act = () => vehicle.Activate();

        act.Should().Throw<InvalidStateTransitionException>();
    }

    // ── Maintenance ───────────────────────────────────────────────────────────

    [Fact]
    public void SendToMaintenance_WhenActive_ShouldBeInMaintenance()
    {
        var vehicle = CreateVehicle();
        vehicle.Activate();

        vehicle.SendToMaintenance();

        vehicle.Status.Should().Be(VehicleStatus.InMaintenance);
    }

    [Fact]
    public void SendToMaintenance_WhenRegistered_ShouldThrow()
    {
        // Cannot skip from Registered directly to InMaintenance
        var vehicle = CreateVehicle();

        Action act = () => vehicle.SendToMaintenance();

        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void CompleteMaintenance_WhenInMaintenance_ShouldBecomeActive()
    {
        var vehicle = CreateVehicle();
        vehicle.Activate();
        vehicle.SendToMaintenance();

        vehicle.CompleteMaintenance();

        vehicle.Status.Should().Be(VehicleStatus.Active);
    }

    [Fact]
    public void CompleteMaintenance_WhenActive_ShouldThrow()
    {
        var vehicle = CreateVehicle();
        vehicle.Activate();

        Action act = () => vehicle.CompleteMaintenance();

        act.Should().Throw<InvalidStateTransitionException>();
    }

    // ── Decommission ──────────────────────────────────────────────────────────

    [Fact]
    public void Decommission_WhenActiveNoDriver_ShouldBeDecommissioned()
    {
        var vehicle = CreateVehicle();
        vehicle.Activate();

        vehicle.Decommission();

        vehicle.Status.Should().Be(VehicleStatus.Decommissioned);
    }

    [Fact]
    public void Decommission_WhenRegistered_ShouldThrow()
    {
        // Cannot decommission from Registered — must go Active first
        var vehicle = CreateVehicle();

        Action act = () => vehicle.Decommission();

        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Decommission_WhenDriverAssigned_ShouldThrow()
    {
        // THE most important test — decommission guard
        var vehicle = CreateVehicle();
        vehicle.Activate();

        var driver = new Driver(
            Guid.NewGuid(), "John", "Doe",
            new LicenseNumber("LB123456"),
            DateTime.UtcNow.AddYears(2));

        vehicle.AssignDriver(driver);

        // Cannot decommission with assigned driver
        Action act = () => vehicle.Decommission();

        act.Should().Throw<DomainException>()
           .WithMessage("*driver*");
    }

    [Fact]
    public void Decommission_WhenInMaintenance_ShouldBeDecommissioned()
    {
        // Can retire a vehicle during maintenance
        var vehicle = CreateVehicle();
        vehicle.Activate();
        vehicle.SendToMaintenance();

        vehicle.Decommission();

        vehicle.Status.Should().Be(VehicleStatus.Decommissioned);
    }

    // ── Domain events ─────────────────────────────────────────────────────────

    [Fact]
    public void Activate_ShouldRaiseDomainEvent()
    {
        var vehicle = CreateVehicle();

        vehicle.Activate();

        vehicle.DomainEvents.Should().ContainSingle()
               .Which.Should().BeOfType<FleetService.Domain.Events.VehicleStatusChangedEvent>();
    }
}