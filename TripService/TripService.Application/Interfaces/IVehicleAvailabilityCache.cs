namespace TripService.Application.Interfaces;

// Trip Service never calls Fleet DB directly.
// This cache holds a local copy of vehicle/driver availability,
// updated later by RabbitMQ events (Task 7).
public interface IVehicleAvailabilityCache
{
    // local read-only cache of fleet data
    Task<bool> IsVehicleAvailableAsync(Guid vehicleId);
    Task<bool> IsDriverAvailableAsync(Guid driverId);
    Task SetVehicleAvailableAsync(Guid vehicleId, bool available);
    Task SetDriverAvailableAsync(Guid driverId, bool available);

}