namespace Shared.Messages;

// Published by Fleet when a driver is assigned to a vehicle
public record DriverAssignedMessage(
    Guid VehicleId,
    Guid DriverId
);