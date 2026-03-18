namespace Shared.Messages;

// Published by Trip — Fleet consumes this to update vehicle mileage
public record TripCompletedMessage(
    Guid    TripId,
    Guid    VehicleId,
    Guid    DriverId,
    decimal DistanceKm
);