using System.Text.Json;
using FleetService.Application.DTOs.Driver;
using FleetService.Application.DTOs.Vehicle;
using FleetService.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace FleetService.Infrastructure.Cache;

public class FleetCacheService : IFleetCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<FleetCacheService> _logger;
    
    // Cache keys
    private const string VehiclesKey = "vehicles:all";
    private const string DriversKey  = "drivers:all";
    
    // How long data stays cached — 5 minutes
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public FleetCacheService(IDistributedCache cache, ILogger<FleetCacheService> logger)
    {
        _cache  = cache;
        _logger = logger;
    }
    
    // ── Vehicles ─────────────────────────────────────────────────────────────

    public async Task<IEnumerable<VehicleDto>?> GetVehiclesAsync()
    {
        var json = await _cache.GetStringAsync(VehiclesKey);
        if (json == null)
        {
            _logger.LogInformation("CACHE MISS: vehicles list");
            return null;
        }
        _logger.LogInformation("CACHE HIT: vehicles list");
        return JsonSerializer.Deserialize<IEnumerable<VehicleDto>>(json);
    }

    public async Task SetVehiclesAsync(IEnumerable<VehicleDto> vehicles)
    {
        var json = JsonSerializer.Serialize(vehicles);
        await _cache.SetStringAsync(VehiclesKey, json, CacheOptions);
        _logger.LogInformation("CACHE SET: vehicles list");
    }

    public async Task InvalidateVehiclesAsync()
    {
        await _cache.RemoveAsync(VehiclesKey);
        _logger.LogInformation("CACHE INVALIDATED: vehicles list");
    }

    // ── Drivers ──────────────────────────────────────────────────────────────

    public async Task<IEnumerable<DriverDto>?> GetDriversAsync()
    {
        var json = await _cache.GetStringAsync(DriversKey);
        if (json == null)
        {
            _logger.LogInformation("CACHE MISS: drivers list");
            return null;
        }
        _logger.LogInformation("CACHE HIT: drivers list");
        return JsonSerializer.Deserialize<IEnumerable<DriverDto>>(json);
    }

    public async Task SetDriversAsync(IEnumerable<DriverDto> drivers)
    {
        var json = JsonSerializer.Serialize(drivers);
        await _cache.SetStringAsync(DriversKey, json, CacheOptions);
        _logger.LogInformation("CACHE SET: drivers list");
    }

    public async Task InvalidateDriversAsync()
    {
        await _cache.RemoveAsync(DriversKey);
        _logger.LogInformation("CACHE INVALIDATED: drivers list");
    }

}