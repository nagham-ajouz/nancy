using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Messages;
using TripService.Application.Interfaces;

namespace TripService.Infrastructure.Messaging.Publishers;

// Called by TripAppService after trip domain events fire
public class TripEventPublisher : ITripEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<TripEventPublisher> _logger;

    public TripEventPublisher(IPublishEndpoint publishEndpoint,  ILogger<TripEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishTripStartedAsync(Guid tripId, Guid vehicleId, Guid driverId)
    {
        await _publishEndpoint.Publish(new TripStartedMessage(tripId, vehicleId, driverId));
        _logger.LogInformation(
            "PUBLISHED: TripStarted | TripId: {TripId} | VehicleId: {VehicleId} | DriverId: {DriverId}",
            tripId, vehicleId, driverId);
    }

    public async Task PublishTripCompletedAsync(Guid tripId, Guid vehicleId, Guid driverId, decimal distanceKm)
    {
        await _publishEndpoint.Publish(new TripCompletedMessage(tripId, vehicleId, driverId, distanceKm));
        _logger.LogInformation(
            "PUBLISHED: TripCompleted | TripId: {TripId} | VehicleId: {VehicleId} | DriverId: {DriverId} | Distance: {DistanceKm}km",
            tripId, vehicleId, driverId, distanceKm);
    }
}