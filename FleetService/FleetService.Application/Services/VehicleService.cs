using FleetService.Application.DTOs.Vehicle;
using FleetService.Application.Interfaces;
using FleetService.Domain.Entities;
using FleetService.Domain.Enums;
using FleetService.Domain.ValueObjects;
using Shared.Exceptions;

namespace FleetService.Application.Services;

public class VehicleService
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IDriverRepository  _driverRepository;

    public VehicleService(IVehicleRepository vehicleRepository, IDriverRepository driverRepository)
    {
        _vehicleRepository = vehicleRepository;
        _driverRepository  = driverRepository;
    }

    // CRUD 

    public async Task<IEnumerable<VehicleDto>> GetAllAsync()
    {
        var vehicles = await _vehicleRepository.GetAllAsync();
        return vehicles.Select(MapToDto);
    }

    public async Task<VehicleDto> GetByIdAsync(Guid id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Vehicle {id} not found.");
        return MapToDto(vehicle);
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleDto dto)
    {
        // Parse enum and value object — invalid values throw here (input validation)
        var type        = Enum.Parse<VehicleType>(dto.Type, ignoreCase: true);
        var plateNumber = new PlateNumber(dto.PlateNumber);

        var vehicle = new Vehicle(Guid.NewGuid(), plateNumber, dto.Model, dto.Year, type);
        await _vehicleRepository.AddAsync(vehicle);
        return MapToDto(vehicle);
    }

    public async Task<VehicleDto> UpdateAsync(Guid id, UpdateVehicleDto dto)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Vehicle {id} not found.");

        // only non-sensitive fields can be updated directly
        vehicle.UpdateDetails(dto.Model, dto.Year);
        await _vehicleRepository.UpdateAsync(vehicle);
        return MapToDto(vehicle);
    }

    public async Task DeleteAsync(Guid id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Vehicle {id} not found.");
        await _vehicleRepository.DeleteAsync(vehicle);
    }

    // State transitions
    
    public async Task<VehicleDto> ActivateAsync(Guid id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Vehicle {id} not found.");
        vehicle.Activate();
        await _vehicleRepository.UpdateAsync(vehicle);
        return MapToDto(vehicle);
    }

    public async Task<VehicleDto> SendToMaintenanceAsync(Guid id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Vehicle {id} not found.");
        vehicle.SendToMaintenance();
        await _vehicleRepository.UpdateAsync(vehicle);
        return MapToDto(vehicle);
    }

    public async Task<VehicleDto> CompleteMaintenanceAsync(Guid id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Vehicle {id} not found.");
        vehicle.CompleteMaintenance();
        await _vehicleRepository.UpdateAsync(vehicle);
        return MapToDto(vehicle);
    }

    public async Task<VehicleDto> DecommissionAsync(Guid id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Vehicle {id} not found.");
        vehicle.Decommission();
        await _vehicleRepository.UpdateAsync(vehicle);
        return MapToDto(vehicle);
    }

    // Assignment 

    public async Task<VehicleDto> AssignDriverAsync(Guid vehicleId, Guid driverId)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId)
            ?? throw new NotFoundException($"Vehicle {vehicleId} not found.");
        var driver = await _driverRepository.GetByIdAsync(driverId)
            ?? throw new NotFoundException($"Driver {driverId} not found.");

        // All business rules enforced inside Vehicle.AssignDriver()
        vehicle.AssignDriver(driver);

        await _vehicleRepository.UpdateAsync(vehicle);
        await _driverRepository.UpdateAsync(driver);
        return MapToDto(vehicle);
    }

    public async Task<VehicleDto> UnassignDriverAsync(Guid vehicleId)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId)
            ?? throw new NotFoundException($"Vehicle {vehicleId} not found.");

        if (!vehicle.DriverId.HasValue)
            throw new DomainException("Vehicle has no assigned driver.");

        var driver = await _driverRepository.GetByIdAsync(vehicle.DriverId.Value)
            ?? throw new NotFoundException($"Driver not found.");

        vehicle.UnassignDriver(driver);

        await _vehicleRepository.UpdateAsync(vehicle);
        await _driverRepository.UpdateAsync(driver);
        return MapToDto(vehicle);
    }

    // Filtering

    public async Task<IEnumerable<VehicleDto>> GetByFilterAsync(string? status, string? type)
    {
        VehicleStatus? statusEnum = null;
        VehicleType? typeEnum = null;

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<VehicleStatus>(status, true, out var parsedStatus))
        {
            statusEnum = parsedStatus;
        }

        if (!string.IsNullOrWhiteSpace(type) &&
            Enum.TryParse<VehicleType>(type, true, out var parsedType))
        {
            typeEnum = parsedType;
        }

        var vehicles = await _vehicleRepository.GetByFilterAsync(statusEnum, typeEnum);

        return vehicles.Select(MapToDto);
    }

    // Mapping

    // Maps entity → DTO, keeps domain model away from the API layer
    private static VehicleDto MapToDto(Vehicle v)
    {
        return new VehicleDto
        {
            Id          = v.Id,
            PlateNumber = v.PlateNumber.Value,
            Model       = v.Model,
            Year        = v.Year,
            Type        = v.Type.ToString(),
            Status      = v.Status.ToString(),
            Mileage     = v.Mileage,
            DriverId    = v.DriverId
        };
    }

}