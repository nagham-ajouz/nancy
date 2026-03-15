namespace FleetService.Domain.Events;

public record DriverUnassignedEvent(Guid VehicleId, Guid DriverId);