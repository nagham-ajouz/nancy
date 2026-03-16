using FleetService.Application.DTOs.Driver;
using FleetService.Application.Interfaces;
using FleetService.Domain.Entities;
using FleetService.Domain.Enums;
using FleetService.Domain.ValueObjects;
using Shared.Exceptions;

namespace FleetService.Application.Services;

public class DriverService
{
    private readonly IDriverRepository _driverRepository;

    public DriverService(IDriverRepository driverRepository)
    {
        _driverRepository = driverRepository;
    }

    public async Task<IEnumerable<DriverDto>> GetAllAsync()
    {
        var drivers = await _driverRepository.GetAllAsync();
        return drivers.Select(MapToDto);
    }

    public async Task<DriverDto> GetByIdAsync(Guid id)
    {
        var driver = await _driverRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Driver {id} not found.");
        return MapToDto(driver);
    }

    public async Task<DriverDto> CreateAsync(CreateDriverDto dto)
    {
        var licenseNumber = new LicenseNumber(dto.LicenseNumber);
        var driver = new Driver(Guid.NewGuid(), dto.FirstName, dto.LastName,
                                licenseNumber, dto.LicenseExpiry);
        await _driverRepository.AddAsync(driver);
        return MapToDto(driver);
    }

    public async Task<DriverDto> UpdateAsync(Guid id, UpdateDriverDto dto)
    {
        var driver = await _driverRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Driver {id} not found.");
        
        driver.UpdateDetails(dto.FirstName, dto.LastName, dto.LicenseExpiry);
        await _driverRepository.UpdateAsync(driver);
        return MapToDto(driver);
    }

    public async Task DeleteAsync(Guid id)
    {
        var driver = await _driverRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Driver {id} not found.");
        await _driverRepository.DeleteAsync(driver);
    }

    public async Task<IEnumerable<DriverDto>> GetByFilterAsync(string? status)
    {
        DriverStatus? statusEnum = status != null
            ? Enum.Parse<DriverStatus>(status, ignoreCase: true) //convert a string → enum.
            : null;
        var drivers = await _driverRepository.GetByFilterAsync(statusEnum);
        return drivers.Select(MapToDto);
    }

    private static DriverDto MapToDto(Driver d)
    {
        return new DriverDto
        {
            Id            = d.Id,
            FirstName     = d.FirstName,
            LastName      = d.LastName,
            LicenseNumber = d.LicenseNumber.Value,
            LicenseExpiry = d.LicenseExpiry,
            Status        = d.Status.ToString(),
            VehicleId     = d.VehicleId
        };
    }
}