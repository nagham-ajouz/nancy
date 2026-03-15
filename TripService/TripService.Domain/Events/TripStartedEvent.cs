namespace TripService.Domain.Events;

public record TripStartedEvent(
    Guid TripId, 
    Guid VehicleId, 
    Guid DriverId
);