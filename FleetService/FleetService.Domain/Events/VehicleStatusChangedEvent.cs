using FleetService.Domain.Enums;

namespace FleetService.Domain.Events;
public record VehicleStatusChangedEvent(
    Guid VehicleId, 
    VehicleStatus NewStatus,
    VehicleType Type);