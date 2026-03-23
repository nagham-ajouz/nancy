using FleetService.Application.DTOs.Driver;
using FleetService.Application.DTOs.Vehicle;

namespace FleetService.Application.Interfaces;

// Caches vehicle and driver lists to avoid hitting DB on every GET
public interface IFleetCacheService
{
    // Vehicles
    Task<IEnumerable<VehicleDto>?> GetVehiclesAsync();
    Task SetVehiclesAsync(IEnumerable<VehicleDto> vehicles);
    Task InvalidateVehiclesAsync();

    // Drivers
    Task<IEnumerable<DriverDto>?> GetDriversAsync();
    Task SetDriversAsync(IEnumerable<DriverDto> drivers);
    Task InvalidateDriversAsync();
}