namespace TripService.Domain.Events;

public record TripCompletedEvent(
    Guid TripId, 
    Guid VehicleId, 
    Guid DriverId, 
    decimal DistanceKm
);