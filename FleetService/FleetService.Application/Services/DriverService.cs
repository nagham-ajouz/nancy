using AutoMapper;
using FleetService.Application.DTOs.Driver;
using FleetService.Application.Interfaces;
using FleetService.Domain.Entities;
using FleetService.Domain.Enums;
using FleetService.Domain.ValueObjects;
using FleetService.Interfaces.Services;
using Shared.Exceptions;

namespace FleetService.Application.Services;

public class DriverService
{
    private readonly IDriverRepository _driverRepository;
    private readonly IMapper           _mapper;
    private readonly IFleetCacheService _cache;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IFleetEventPublisher _publisher;

    public DriverService(
        IDriverRepository driverRepository, 
        IMapper mapper,
        IFleetCacheService cache,
        IDomainEventDispatcher dispatcher,
        IFleetEventPublisher publisher)
    {
        _driverRepository = driverRepository;
        _mapper           = mapper;
        _cache = cache;
        _dispatcher = dispatcher;
        _publisher = publisher;
    }

    public async Task<IEnumerable<DriverDto>> GetAllAsync()
    {
        var cached = await _cache.GetDriversAsync();
        if (cached != null)
            return cached;

        var drivers = await _driverRepository.GetAllAsync();
        var dtos    = _mapper.Map<IEnumerable<DriverDto>>(drivers);
        await _cache.SetDriversAsync(dtos);
        return dtos;
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
        await _dispatcher.DispatchAsync(driver);
        
        await _publisher.PublishDriverLicenseExpiryAsync(driver.Id, driver.LicenseExpiry);
        
        await _cache.InvalidateDriversAsync();
        return _mapper.Map<DriverDto>(driver);
    }

    public async Task<DriverDto> UpdateAsync(Guid id, UpdateDriverDto dto)
    {
        var driver = await _driverRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Driver {id} not found.");
        driver.UpdateDetails(dto.FirstName, dto.LastName, dto.LicenseExpiry);
        await _driverRepository.UpdateAsync(driver);
        await _dispatcher.DispatchAsync(driver);
        await _cache.InvalidateDriversAsync();
        return _mapper.Map<DriverDto>(driver);
    }

    public async Task DeleteAsync(Guid id)
    {
        var driver = await _driverRepository.GetByIdAsync(id)
                     ?? throw new NotFoundException($"Driver {id} not found.");
        
        await _driverRepository.DeleteAsync(driver);
        await _cache.InvalidateDriversAsync();
    }

    public async Task<IEnumerable<DriverDto>> GetByFilterAsync(string? status)
    {
        DriverStatus? statusEnum = status != null
            ? Enum.Parse<DriverStatus>(status, ignoreCase: true)
            : null;
        var drivers = await _driverRepository.GetByFilterAsync(statusEnum);
        return _mapper.Map<IEnumerable<DriverDto>>(drivers);
    }
    
    public async Task<DriverDto> DeactivateAsync(Guid id)
    {
        var driver = await _driverRepository.GetByIdAsync(id)
                     ?? throw new NotFoundException($"Driver {id} not found.");
        driver.Deactivate();
        await _driverRepository.UpdateAsync(driver);
        await _dispatcher.DispatchAsync(driver); 
        await _cache.InvalidateDriversAsync();
        return _mapper.Map<DriverDto>(driver);
    }
}