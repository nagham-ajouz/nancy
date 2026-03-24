namespace Shared.Messages;

// Published by Fleet when a vehicle transitions state
public record VehicleStatusChangedMessage(
    Guid   VehicleId,
    string NewStatus,  // "Active", "InMaintenance", "Decommissioned"
    string VehicleType
);