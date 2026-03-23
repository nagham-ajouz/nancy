using AutoMapper;
using FleetService.Application.DTOs.Vehicle;
using FleetService.Application.Interfaces;
using FleetService.Domain.Entities;
using FleetService.Domain.Enums;
using FleetService.Domain.ValueObjects;
using FleetService.Interfaces.Services;
using Shared.Exceptions;

namespace FleetService.Application.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository  _vehicleRepository;
    private readonly IDriverRepository   _driverRepository;
    private readonly IMapper             _mapper;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IFleetCacheService _cache;

    public VehicleService(
        IVehicleRepository vehicleRepository,
        IDriverRepository driverRepository,
        IDomainEventDispatcher dispatcher,
        IFleetCacheService cache,
        IMapper mapper)
    {
        _vehicleRepository = vehicleRepository;
        _driverRepository  = driverRepository;
        _mapper            = mapper;
        _cache             = cache;
        _dispatcher = dispatcher;
    }

    public async Task<IEnumerable<VehicleDto>> GetAllAsync()
    {
        var cached = await _cache.GetVehiclesAsync();
        if (cached != null)
            return cached;

        // Cache miss — hit DB, then cache result
        var vehicles = await _vehicleRepository.GetAllAsync();
        var dtos     = _mapper.Map<IEnumerable<VehicleDto>>(vehicles);
        await _cache.SetVehiclesAsync(dtos);
        return dtos;
    }

    public async Task<VehicleDto> GetByIdAsync(Guid id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Vehicle {id} not found.");
        return _mapper.Map<VehicleDto>(vehicle);
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleDto dto)
    {
        var type        = Enum.Parse<VehicleType>(dto.Type, ignoreCase: true);
        var plateNumber = new PlateNumber(dto.PlateNumber);
        var vehicle     = new Vehicle(Guid.NewGuid(), plateNumber, dto.Model, dto.Year, type);
        await _vehicleRepository.AddAsync(vehicle);
        await _cache.InvalidateVehiclesAsync();
        return _mapper.Map<VehicleDto>(vehicle);
    }

    public async Task<VehicleDto> UpdateAsync(Guid id, UpdateVehicleDto dto)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Vehicle {id} not found.");
        vehicle.UpdateDetails(dto.Model, dto.Year);
        await _vehicleRepository.UpdateAsync(vehicle);
        await _cache.InvalidateVehiclesAsync();
        return _mapper.Map<VehicleDto>(vehicle);
    }

    public async Task DeleteAsync(Guid id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Vehicle {id} not found.");
        await _vehicleRepository.DeleteAsync(vehicle);
        await _cache.InvalidateVehiclesAsync(); // ← invalidate after delete
    }

    public async Task<VehicleDto> ActivateAsync(Guid id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Vehicle {id} not found.");
        vehicle.Activate();
        await _vehicleRepository.UpdateAsync(vehicle);
        await _dispatcher.DispatchAsync(vehicle);
        await _cache.InvalidateVehiclesAsync(); // ← invalidate after status change
        return _mapper.Map<VehicleDto>(vehicle);
    }

    public async Task<VehicleDto> SendToMaintenanceAsync(Guid id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Vehicle {id} not found.");
        vehicle.SendToMaintenance();
        await _vehicleRepository.UpdateAsync(vehicle);
        // await _publisher.PublishVehicleStatusChangedAsync(vehicle.Id, vehicle.Status.ToString());
        await _dispatcher.DispatchAsync(vehicle);
        await _cache.InvalidateVehiclesAsync();
        return _mapper.Map<VehicleDto>(vehicle);
    }

    public async Task<VehicleDto> CompleteMaintenanceAsync(Guid id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Vehicle {id} not found.");
        vehicle.CompleteMaintenance();
        await _vehicleRepository.UpdateAsync(vehicle);
        await _dispatcher.DispatchAsync(vehicle);
        await _cache.InvalidateVehiclesAsync();
        return _mapper.Map<VehicleDto>(vehicle);
    }

    public async Task<VehicleDto> DecommissionAsync(Guid id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Vehicle {id} not found.");
        vehicle.Decommission();
        await _vehicleRepository.UpdateAsync(vehicle);
        await _dispatcher.DispatchAsync(vehicle);
        await _cache.InvalidateVehiclesAsync();
        return _mapper.Map<VehicleDto>(vehicle);
    }

    public async Task<VehicleDto> AssignDriverAsync(Guid vehicleId, Guid driverId)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId)
            ?? throw new NotFoundException($"Vehicle {vehicleId} not found.");
        var driver = await _driverRepository.GetByIdAsync(driverId)
            ?? throw new NotFoundException($"Driver {driverId} not found.");
        vehicle.AssignDriver(driver);
        await _vehicleRepository.UpdateAsync(vehicle);
        await _driverRepository.UpdateAsync(driver);
        await _dispatcher.DispatchAsync(vehicle);
        await _cache.InvalidateVehiclesAsync();
        return _mapper.Map<VehicleDto>(vehicle);
    }

    public async Task<VehicleDto> UnassignDriverAsync(Guid vehicleId)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId)
            ?? throw new NotFoundException($"Vehicle {vehicleId} not found.");
        if (!vehicle.DriverId.HasValue)
            throw new DomainException("Vehicle has no assigned driver.");
        var driver = await _driverRepository.GetByIdAsync(vehicle.DriverId.Value)
            ?? throw new NotFoundException("Driver not found.");
        vehicle.UnassignDriver(driver);
        await _vehicleRepository.UpdateAsync(vehicle);
        await _driverRepository.UpdateAsync(driver);
        await _dispatcher.DispatchAsync(vehicle);
        await _cache.InvalidateVehiclesAsync();
        return _mapper.Map<VehicleDto>(vehicle);
    }

    public async Task<IEnumerable<VehicleDto>> GetByFilterAsync(string? status, string? type)
    {
        VehicleStatus? statusEnum = null;
        VehicleType?   typeEnum   = null;
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<VehicleStatus>(status, true, out var ps)) statusEnum = ps;
        if (!string.IsNullOrWhiteSpace(type) &&
            Enum.TryParse<VehicleType>(type, true, out var pt))     typeEnum   = pt;
        var vehicles = await _vehicleRepository.GetByFilterAsync(statusEnum, typeEnum);
        return _mapper.Map<IEnumerable<VehicleDto>>(vehicles);
    }
}