using AutoMapper;
using Shared.Exceptions;
using Shared.ValueObjects;
using TripService.Application.DTOs.Trip;
using TripService.Application.DTOs.TripLog;
using TripService.Application.Interfaces;
using TripService.Domain.Entities;
using TripService.Domain.Enums;

namespace TripService.Application.Services;

public class TripAppService
{
    private readonly ITripRepository           _tripRepository;
    private readonly IVehicleAvailabilityCache _availabilityCache;
    private readonly IMapper                   _mapper;
    private readonly DomainEventDispatcher _dispatcher;

    public TripAppService(ITripRepository tripRepository,
        IVehicleAvailabilityCache availabilityCache,
        DomainEventDispatcher dispatcher,
        IMapper mapper)
    {
        _tripRepository    = tripRepository;
        _availabilityCache = availabilityCache;
        _dispatcher = dispatcher;
        _mapper            = mapper;
    }

    public async Task<TripDto> CreateAsync(CreateTripDto dto)
    {
        var startLocation = new Location(dto.StartLocation.Latitude,
                                         dto.StartLocation.Longitude,
                                         dto.StartLocation.Address);
        var endLocation = new Location(dto.EndLocation.Latitude,
                                       dto.EndLocation.Longitude,
                                       dto.EndLocation.Address);
        var trip = new Trip(Guid.NewGuid(), startLocation, endLocation);
        await _tripRepository.AddAsync(trip);
        return _mapper.Map<TripDto>(trip);
    }

    public async Task<TripDto> GetByIdAsync(Guid id)
    {
        var trip = await _tripRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Trip {id} not found.");
        return _mapper.Map<TripDto>(trip);
    }

    public async Task<IEnumerable<TripDto>> GetByFilterAsync(TripFilterDto filter)
    {
        TripStatus? status = filter.Status != null
            ? Enum.Parse<TripStatus>(filter.Status, ignoreCase: true)
            : null;
        var trips = await _tripRepository.GetByFilterAsync(
            filter.DriverId, filter.VehicleId, status, filter.From, filter.To);
        return _mapper.Map<IEnumerable<TripDto>>(trips);
    }

    public async Task<TripDto> AssignResourcesAsync(Guid tripId, AssignTripDto dto)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId)
                   ?? throw new NotFoundException($"Trip {tripId} not found.");

        // Check vehicle availability
        bool? vehicleAvailable = await _availabilityCache.IsVehicleAvailableAsync(dto.VehicleId);

        if (vehicleAvailable == null)
            throw new DomainException(
                $"Vehicle {dto.VehicleId} status is unknown. " +
                "Ensure Fleet Service is running and has published vehicle status events.");

        if (vehicleAvailable == false)
            throw new DomainException($"Vehicle {dto.VehicleId} is not available for trips.");

        // Check driver availability
        bool? driverAvailable = await _availabilityCache.IsDriverAvailableAsync(dto.DriverId);

        if (driverAvailable == null)
            throw new DomainException(
                $"Driver {dto.DriverId} status is unknown. " +
                "Ensure Fleet Service is running and has published driver status events.");

        if (driverAvailable == false)
            throw new DomainException($"Driver {dto.DriverId} is not available for trips.");

        trip.AssignResources(dto.VehicleId, dto.DriverId);
        await _tripRepository.UpdateAsync(trip);
        return _mapper.Map<TripDto>(trip);
    }

    public async Task<TripDto> StartAsync(Guid tripId)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId)
            ?? throw new NotFoundException($"Trip {tripId} not found.");
        trip.Start();
        await _tripRepository.UpdateAsync(trip);
        await _dispatcher.DispatchAsync(trip); 
        return _mapper.Map<TripDto>(trip);
    }

    public async Task<TripDto> CompleteAsync(Guid tripId)
    {
        var trip = await _tripRepository.GetByIdWithLogsAsync(tripId)
            ?? throw new NotFoundException($"Trip {tripId} not found.");
        trip.Complete();
        await _tripRepository.UpdateAsync(trip);
        await _dispatcher.DispatchAsync(trip); 
        return _mapper.Map<TripDto>(trip);
    }

    public async Task<TripLogDto> AddLogAsync(Guid tripId, AddTripLogDto dto)
    {
        var trip = await _tripRepository.GetByIdWithLogsAsync(tripId)
            ?? throw new NotFoundException($"Trip {tripId} not found.");
        var location = new Location(dto.Latitude, dto.Longitude, dto.Address);
        var log = trip.AddLog(Guid.NewGuid(), location, dto.Timestamp, dto.Speed);
        await _tripRepository.AddLogAsync(log);
        return _mapper.Map<TripLogDto>(log);
    }
    
    public async Task<bool> IsDriverAssignedToTripAsync(Guid tripId, Guid driverId)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId);

        if (trip == null)
            throw new NotFoundException($"Trip {tripId} not found.");

        if (trip.Status != TripStatus.InProgress)
            throw new DomainException("Trip is not in progress.");

        return trip.DriverId == driverId;
    }
    
    public async Task<TripDto> InvoiceAsync(Guid tripId, InvoiceTripDto dto)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId)
                   ?? throw new NotFoundException($"Trip {tripId} not found.");

        // Money is a value object — validates amount >= 0 and currency not empty
        var cost = new Money(dto.Amount, dto.Currency);

        // Domain method enforces Completed → Invoiced transition
        trip.Invoice(cost);

        await _tripRepository.UpdateAsync(trip);
        return _mapper.Map<TripDto>(trip);
    }
}