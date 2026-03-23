using FleetService.Application.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Messages;

namespace FleetService.Infrastructure.Messaging.Consumers;

// Updates vehicle mileage and marks driver available when trip completes
public class TripCompletedConsumer : IConsumer<TripCompletedMessage>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IDriverRepository  _driverRepository;
    private readonly ILogger<TripCompletedConsumer> _logger;
    private readonly IFleetCacheService _cache; 

    public TripCompletedConsumer(
        IVehicleRepository vehicleRepository,
        IDriverRepository  driverRepository,
        ILogger<TripCompletedConsumer> logger,
        IFleetCacheService cache)
    {
        _vehicleRepository = vehicleRepository;
        _driverRepository  = driverRepository;
        _logger            = logger;
        _cache             = cache;
    }

    public async Task Consume(ConsumeContext<TripCompletedMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received TripCompleted for vehicle {VehicleId}, distance {DistanceKm}km",
            message.VehicleId, message.DistanceKm);

        var vehicle = await _vehicleRepository.GetByIdAsync(message.VehicleId);
        if (vehicle is null)
        {
            _logger.LogWarning("Vehicle {VehicleId} not found when processing TripCompleted", message.VehicleId);
            return;
        }

        var driver = await _driverRepository.GetByIdAsync(message.DriverId);
        if (driver is null)
        {
            _logger.LogWarning("Driver {DriverId} not found when processing TripCompleted", message.DriverId);
            return;
        }

        // Domain methods enforce the rules
        vehicle.AddMileage(message.DistanceKm);
        driver.MarkAvailable();

        await _vehicleRepository.UpdateAsync(vehicle);
        await _driverRepository.UpdateAsync(driver);

        // Invalidate cache because vehicle mileage and driver status changed
        await _cache.InvalidateVehiclesAsync();
        await _cache.InvalidateDriversAsync();

        _logger.LogInformation(
            "CACHE INVALIDATED after TripCompleted | Vehicle: {VehicleId} | Driver: {DriverId}",
            message.VehicleId, message.DriverId);
    }
}