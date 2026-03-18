using System.Collections.Concurrent;
using TripService.Application.Interfaces;

namespace TripService.Infrastructure.Cache;

// Stores vehicle/driver availability in memory.
// Task 7 will update this cache when Fleet Service publishes events via RabbitMQ.
public class VehicleAvailabilityCache : IVehicleAvailabilityCache
{
    // key = entity Id, value = is available
    private readonly ConcurrentDictionary<Guid, bool> _vehicleAvailability = new();
    private readonly ConcurrentDictionary<Guid, bool> _driverAvailability  = new();

    public Task<bool> IsVehicleAvailableAsync(Guid vehicleId)
    {
        // Default to true if not in cache yet — will be fixed in Task 7
        bool available = _vehicleAvailability.TryGetValue(vehicleId, out bool val) ? val : true;
        return Task.FromResult(available);
    }

    public Task<bool> IsDriverAvailableAsync(Guid driverId)
    {
        bool available = _driverAvailability.TryGetValue(driverId, out bool val) ? val : true;
        return Task.FromResult(available);
    }

    public Task SetVehicleAvailableAsync(Guid vehicleId, bool available)
    {
        _vehicleAvailability[vehicleId] = available;
        return Task.CompletedTask;
    }

    public Task SetDriverAvailableAsync(Guid driverId, bool available)
    {
        _driverAvailability[driverId] = available;
        return Task.CompletedTask;
    }
    
}