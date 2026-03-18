using MassTransit;
using Shared.Messages;
using TripService.Application.Interfaces;

namespace TripService.Infrastructure.Messaging.Publishers;

// Called by TripAppService after trip domain events fire
public class TripEventPublisher : ITripEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public TripEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishTripStartedAsync(Guid tripId, Guid vehicleId, Guid driverId)
    {
        await _publishEndpoint.Publish(new TripStartedMessage(tripId, vehicleId, driverId));
    }

    public async Task PublishTripCompletedAsync(Guid tripId, Guid vehicleId, Guid driverId, decimal distanceKm)
    {
        await _publishEndpoint.Publish(new TripCompletedMessage(tripId, vehicleId, driverId, distanceKm));
    }
}