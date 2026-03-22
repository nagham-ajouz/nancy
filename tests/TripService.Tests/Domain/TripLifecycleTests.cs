using FluentAssertions;
using Shared.Exceptions;
using Shared.ValueObjects;
using TripService.Domain.Entities;
using TripService.Domain.Enums;
using TripService.Domain.Events;
using TripService.Domain.Exceptions;
using Xunit;

namespace TripService.Tests.Domain;

public class TripLifecycleTests
{
    private static Location MakeLocation(string address = "Test")
        => new(33.88, 35.51, address);

    private static Trip CreateRequestedTrip()
        => new(Guid.NewGuid(), MakeLocation("Start"), MakeLocation("End"));

    private static Trip CreateAssignedTrip()
    {
        var trip = CreateRequestedTrip();
        trip.AssignResources(Guid.NewGuid(), Guid.NewGuid());
        return trip;
    }

    private static Trip CreateInProgressTrip()
    {
        var trip = CreateAssignedTrip();
        trip.Start();
        return trip;
    }

    // ── Assign ────────────────────────────────────────────────────────────────

    [Fact]
    public void AssignResources_WhenRequested_ShouldBeAssigned()
    {
        var trip = CreateRequestedTrip();

        trip.AssignResources(Guid.NewGuid(), Guid.NewGuid());

        trip.Status.Should().Be(TripStatus.Assigned);
        trip.VehicleId.Should().NotBeEmpty();
        trip.DriverId.Should().NotBeEmpty();
    }

    [Fact]
    public void AssignResources_WhenAlreadyAssigned_ShouldThrow()
    {
        var trip = CreateAssignedTrip();

        // Cannot assign again
        Action act = () => trip.AssignResources(Guid.NewGuid(), Guid.NewGuid());

        act.Should().Throw<InvalidTripStateTransitionException>();
    }

    // ── Start ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Start_WhenAssigned_ShouldBeInProgress()
    {
        var trip = CreateAssignedTrip();

        trip.Start();

        trip.Status.Should().Be(TripStatus.InProgress);
        trip.StartTime.Should().NotBeNull();
    }

    [Fact]
    public void Start_WhenNotAssigned_ShouldThrow()
    {
        // Cannot start a trip that was never assigned
        var trip = CreateRequestedTrip();

        Action act = () => trip.Start();

        act.Should().Throw<InvalidTripStateTransitionException>();
    }

    [Fact]
    public void Start_ShouldRaiseTripStartedEvent()
    {
        var trip = CreateAssignedTrip();

        trip.Start();

        trip.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TripStartedEvent>();
    }

    // ── Complete ──────────────────────────────────────────────────────────────

    [Fact]
    public void Complete_WhenInProgress_ShouldBeCompleted()
    {
        var trip = CreateInProgressTrip();

        trip.Complete();

        trip.Status.Should().Be(TripStatus.Completed);
        trip.EndTime.Should().NotBeNull();
    }

    [Fact]
    public void Complete_WhenNotInProgress_ShouldThrow()
    {
        var trip = CreateAssignedTrip();

        // Cannot complete without starting
        Action act = () => trip.Complete();

        act.Should().Throw<InvalidTripStateTransitionException>();
    }

    [Fact]
    public void Complete_ShouldRaiseTripCompletedEvent()
    {
        var trip = CreateInProgressTrip();

        trip.Complete();

        trip.DomainEvents.Should().Contain(e => e is TripCompletedEvent);
    }

    [Fact]
    public void Complete_WithTwoLogs_ShouldCalculateDistance()
    {
        var trip = CreateInProgressTrip();

        // Beirut → Jounieh ~13km
        trip.AddLog(Guid.NewGuid(), new Location(33.8869, 35.5131, "Beirut"),
                    DateTime.UtcNow, 0);
        trip.AddLog(Guid.NewGuid(), new Location(33.9831, 35.5731, "Jounieh"),
                    DateTime.UtcNow.AddMinutes(30), 80);

        trip.Complete();

        trip.DistanceKm.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Complete_WithNoLogs_ShouldHaveZeroDistance()
    {
        var trip = CreateInProgressTrip();

        trip.Complete();

        trip.DistanceKm.Should().Be(0);
    }

    // ── Invoice ───────────────────────────────────────────────────────────────

    [Fact]
    public void Invoice_WhenCompleted_ShouldBeInvoiced()
    {
        var trip = CreateInProgressTrip();
        trip.Complete();

        trip.Invoice(new Money(50, "USD"));

        trip.Status.Should().Be(TripStatus.Invoiced);
        trip.Cost!.Amount.Should().Be(50);
        trip.Cost!.Currency.Should().Be("USD");
    }

    [Fact]
    public void Invoice_WhenNotCompleted_ShouldThrow()
    {
        var trip = CreateInProgressTrip();

        // Cannot invoice without completing first
        Action act = () => trip.Invoice(new Money(50, "USD"));

        act.Should().Throw<InvalidTripStateTransitionException>();
    }

    // ── AddLog ────────────────────────────────────────────────────────────────

    [Fact]
    public void AddLog_WhenInProgress_ShouldAddToCollection()
    {
        var trip = CreateInProgressTrip();

        trip.AddLog(Guid.NewGuid(), MakeLocation(), DateTime.UtcNow, 60);

        trip.Logs.Should().HaveCount(1);
    }

    [Fact]
    public void AddLog_WhenNotInProgress_ShouldThrow()
    {
        var trip = CreateAssignedTrip();

        Action act = () => trip.AddLog(Guid.NewGuid(), MakeLocation(), DateTime.UtcNow, 60);

        act.Should().Throw<DomainException>()
           .WithMessage("*InProgress*");
    }
}