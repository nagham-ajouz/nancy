using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TripService.Application.Interfaces;

namespace TripService.Infrastructure.Cache;

public class VehicleAvailabilityCache : IVehicleAvailabilityCache
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<VehicleAvailabilityCache> _logger;

    private const string VehiclePrefix = "availability:vehicle:";
    private const string DriverPrefix  = "availability:driver:";
    private const string VehicleTypePrefix = "vehicletype:";

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
    };

    public VehicleAvailabilityCache(IDistributedCache cache, ILogger<VehicleAvailabilityCache> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    public async Task<bool?> IsVehicleAvailableAsync(Guid vehicleId)
    {
        var value = await _cache.GetStringAsync(VehiclePrefix + vehicleId);

        if (value == null)
        {
            // Not in cache — we genuinely don't know this vehicle's status
            _logger.LogWarning(
                "CACHE MISS: vehicle {VehicleId} not in cache — status unknown",
                vehicleId);
            return null;
        }

        return bool.Parse(value);
    }

    public async Task<bool?> IsDriverAvailableAsync(Guid driverId)
    {
        var value = await _cache.GetStringAsync(DriverPrefix + driverId);

        if (value == null)
        {
            _logger.LogWarning(
                "CACHE MISS: driver {DriverId} not in cache — status unknown",
                driverId);
            return null;
        }

        return bool.Parse(value);
    }

    public async Task SetVehicleAvailableAsync(Guid vehicleId, bool available)
    {
        await _cache.SetStringAsync(
            VehiclePrefix + vehicleId,
            available.ToString(),
            CacheOptions);
        
        _logger.LogInformation(
            "CACHE SET: vehicle {VehicleId} → available: {Available}",
            vehicleId, available);
    }

    public async Task SetDriverAvailableAsync(Guid driverId, bool available)
    {
        await _cache.SetStringAsync(
            DriverPrefix + driverId,
            available.ToString(),
            CacheOptions);

        _logger.LogInformation(
            "CACHE SET: driver {DriverId} → available: {Available}",
            driverId, available);
    }

    public async Task SetVehicleTypeAsync(Guid vehicleId, string vehicleType)
    {
        await _cache.SetStringAsync(
            VehicleTypePrefix + vehicleId,
            vehicleType,
            CacheOptions);
    }

    public async Task<string?> GetVehicleTypeAsync(Guid vehicleId)
    {
        return await _cache.GetStringAsync(VehicleTypePrefix + vehicleId);
    }
}