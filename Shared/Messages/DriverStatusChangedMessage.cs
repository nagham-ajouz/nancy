namespace Shared.Messages;

// Published by Fleet when a driver becomes Available or Inactive
public record DriverStatusChangedMessage(
    Guid   DriverId,
    string NewStatus  // "Available", "OnTrip", "Inactive"
);