namespace Shared.Messages;

// Published by Trip — Fleet consumes this to mark driver as OnTrip
public record TripStartedMessage(
    Guid TripId,
    Guid VehicleId,
    Guid DriverId
);