namespace Shared.Messages;

public record DriverUnassignedMessage(
    Guid VehicleId,
    Guid DriverId
);