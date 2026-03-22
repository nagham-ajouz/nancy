using FleetService.Application.DTOs.Vehicle;

namespace FleetService.Application.Interfaces;

public interface IVehicleService
{
    Task<IEnumerable<VehicleDto>> GetAllAsync();
    Task<VehicleDto>              GetByIdAsync(Guid id);
    Task<VehicleDto>              CreateAsync(CreateVehicleDto dto);
    Task<VehicleDto>              UpdateAsync(Guid id, UpdateVehicleDto dto);
    Task                          DeleteAsync(Guid id);
    Task<VehicleDto>              ActivateAsync(Guid id);
    Task<VehicleDto>              SendToMaintenanceAsync(Guid id);
    Task<VehicleDto>              CompleteMaintenanceAsync(Guid id);
    Task<VehicleDto>              DecommissionAsync(Guid id);
    Task<VehicleDto>              AssignDriverAsync(Guid vehicleId, Guid driverId);
    Task<VehicleDto>              UnassignDriverAsync(Guid vehicleId);
    Task<IEnumerable<VehicleDto>> GetByFilterAsync(string? status, string? type);
}