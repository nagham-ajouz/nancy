using FleetService.Domain.Enums;

namespace FleetService.Domain.Events;

public record DriverStatusChangedEvent (Guid DriverId, DriverStatus NewStatus);