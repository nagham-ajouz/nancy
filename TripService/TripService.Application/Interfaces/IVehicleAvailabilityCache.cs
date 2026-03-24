namespace TripService.Application.Interfaces;

public interface IVehicleAvailabilityCache
{
    // local read-only cache of fleet data
    Task<bool?> IsVehicleAvailableAsync(Guid vehicleId);
    Task<bool?> IsDriverAvailableAsync(Guid driverId);
    Task SetVehicleAvailableAsync(Guid vehicleId, bool available);
    Task SetDriverAvailableAsync(Guid driverId, bool available);
    Task SetVehicleTypeAsync(Guid vehicleId, string vehicleType);
    Task<string?> GetVehicleTypeAsync(Guid vehicleId);

}