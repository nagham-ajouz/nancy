namespace FleetService.Domain.Events;

public record DriverAssignedEvent(Guid VehicleId, Guid DriverId);