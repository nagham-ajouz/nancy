using Shared.Exceptions;
using Shared.ValueObjects;
using TripService.Application.DTOs;
using TripService.Application.DTOs.Trip;
using TripService.Application.DTOs.TripLog;
using TripService.Application.Interfaces;
using TripService.Domain.Entities;
using TripService.Domain.Enums;

namespace TripService.Application.Services;

public class TripAppService
{
    private readonly ITripRepository _tripRepository;
    private readonly IVehicleAvailabilityCache _availabilityCache;
    private readonly ITripEventPublisher _publisher;

    public TripAppService(ITripRepository tripRepository, 
        IVehicleAvailabilityCache availabilityCache,
        ITripEventPublisher publisher)
    {
        _tripRepository    = tripRepository;
        _availabilityCache = availabilityCache;
        _publisher         = publisher;
    }

    
    // Creates a trip in Requested state — no vehicle/driver yet
    public async Task<TripDto> CreateAsync(CreateTripDto dto)
    {
        var startLocation = new Location(dto.StartLocation.Latitude,
                                         dto.StartLocation.Longitude,
                                         dto.StartLocation.Address);
        var endLocation   = new Location(dto.EndLocation.Latitude,
                                         dto.EndLocation.Longitude,
                                         dto.EndLocation.Address);

        var trip = new Trip(Guid.NewGuid(), startLocation, endLocation);
        await _tripRepository.AddAsync(trip);
        return MapToDto(trip);
    }

    // Get by id

    public async Task<TripDto> GetByIdAsync(Guid id)
    {
        var trip = await _tripRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Trip {id} not found.");
        return MapToDto(trip);
    }

    // Filter 

    public async Task<IEnumerable<TripDto>> GetByFilterAsync(TripFilterDto filter)
    {
        TripStatus? status = filter.Status != null
            ? Enum.Parse<TripStatus>(filter.Status, ignoreCase: true)
            : null;

        var trips = await _tripRepository.GetByFilterAsync(
            filter.DriverId,
            filter.VehicleId,
            status,
            filter.From,
            filter.To);

        return trips.Select(MapToDto);
    }

    // Assign vehicle and driver

    public async Task<TripDto> AssignResourcesAsync(Guid tripId, AssignTripDto dto)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId)
            ?? throw new NotFoundException($"Trip {tripId} not found.");

        // Check local availability cache — no direct Fleet DB access
        bool vehicleAvailable = await _availabilityCache.IsVehicleAvailableAsync(dto.VehicleId);
        bool driverAvailable  = await _availabilityCache.IsDriverAvailableAsync(dto.DriverId);

        if (!vehicleAvailable)
            throw new DomainException($"Vehicle {dto.VehicleId} is not available.");

        if (!driverAvailable)
            throw new DomainException($"Driver {dto.DriverId} is not available.");

        // Domain method enforces Requested → Assigned transition
        trip.AssignResources(dto.VehicleId, dto.DriverId);
        await _tripRepository.UpdateAsync(trip);
        return MapToDto(trip);
    }

    // Start
    public async Task<TripDto> StartAsync(Guid tripId)
    {
        var trip = await _tripRepository.GetByIdAsync(tripId)
                   ?? throw new NotFoundException($"Trip {tripId} not found.");

        trip.Start();
        await _tripRepository.UpdateAsync(trip);

        // Publish to RabbitMQ — Fleet will mark driver as OnTrip
        await _publisher.PublishTripStartedAsync(trip.Id, trip.VehicleId, trip.DriverId);

        return MapToDto(trip);
    }

    // Complete

    public async Task<TripDto> CompleteAsync(Guid tripId)
    {
        var trip = await _tripRepository.GetByIdWithLogsAsync(tripId)
                   ?? throw new NotFoundException($"Trip {tripId} not found.");

        trip.Complete();
        await _tripRepository.UpdateAsync(trip);

        // Publish to RabbitMQ — Fleet will update mileage and mark driver available
        await _publisher.PublishTripCompletedAsync(
            trip.Id, trip.VehicleId, trip.DriverId, trip.DistanceKm ?? 0);

        return MapToDto(trip);
    }

    // Add GPS log 

    public async Task<TripLogDto> AddLogAsync(Guid tripId, AddTripLogDto dto)
    {
        // Always load with logs when adding a log
        var trip = await _tripRepository.GetByIdWithLogsAsync(tripId)
                   ?? throw new NotFoundException($"Trip {tripId} not found.");

        var location = new Location(dto.Latitude, dto.Longitude, dto.Address);
        var log = trip.AddLog(Guid.NewGuid(), location, dto.Timestamp, dto.Speed);
    
        // Save the log directly instead of updating the whole trip
        await _tripRepository.AddLogAsync(log);
    
        return MapLogToDto(log);
    }

    // Mapping 

    private static TripDto MapToDto(Trip t)
    {
        return new TripDto
        {
            Id            = t.Id,
            VehicleId     = t.VehicleId,
            DriverId      = t.DriverId,
            Status        = t.Status.ToString(),
            StartLocation = new LocationDto
            {
                Latitude  = t.StartLocation.Latitude,
                Longitude = t.StartLocation.Longitude,
                Address   = t.StartLocation.Address
            },
            EndLocation = new LocationDto
            {
                Latitude  = t.EndLocation.Latitude,
                Longitude = t.EndLocation.Longitude,
                Address   = t.EndLocation.Address
            },
            StartTime    = t.StartTime,
            EndTime      = t.EndTime,
            DistanceKm   = t.DistanceKm,
            CostAmount   = t.Cost?.Amount,
            CostCurrency = t.Cost?.Currency
        };
    }

    private static TripLogDto MapLogToDto(TripLog l)
    {
        return new TripLogDto
        {
            Id        = l.Id,
            TripId    = l.TripId,
            Latitude  = l.Location.Latitude,
            Longitude = l.Location.Longitude,
            Address   = l.Location.Address,
            Timestamp = l.Timestamp,
            Speed     = l.Speed
        };
    }
}