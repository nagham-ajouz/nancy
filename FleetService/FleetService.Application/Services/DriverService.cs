using AutoMapper;
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
    private readonly IMapper           _mapper;

    public DriverService(IDriverRepository driverRepository, IMapper mapper)
    {
        _driverRepository = driverRepository;
        _mapper           = mapper;
    }

    public async Task<IEnumerable<DriverDto>> GetAllAsync()
    {
        var drivers = await _driverRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<DriverDto>>(drivers);
    }

    public async Task<DriverDto> GetByIdAsync(Guid id)
    {
        var driver = await _driverRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Driver {id} not found.");
        return _mapper.Map<DriverDto>(driver);
    }

    public async Task<DriverDto> CreateAsync(CreateDriverDto dto)
    {
        var licenseNumber = new LicenseNumber(dto.LicenseNumber);
        var driver = new Driver(Guid.NewGuid(), 
            dto.FirstName, 
            dto.LastName,
            licenseNumber, 
            dto.LicenseExpiry);
        await _driverRepository.AddAsync(driver);
        return _mapper.Map<DriverDto>(driver);
    }

    public async Task<DriverDto> UpdateAsync(Guid id, UpdateDriverDto dto)
    {
        var driver = await _driverRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Driver {id} not found.");
        driver.UpdateDetails(dto.FirstName, dto.LastName, dto.LicenseExpiry);
        await _driverRepository.UpdateAsync(driver);
        return _mapper.Map<DriverDto>(driver);
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
            ? Enum.Parse<DriverStatus>(status, ignoreCase: true)
            : null;
        var drivers = await _driverRepository.GetByFilterAsync(statusEnum);
        return _mapper.Map<IEnumerable<DriverDto>>(drivers);
    }
}