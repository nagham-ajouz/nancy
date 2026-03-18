namespace TripService.Application.Interfaces;

public interface ITripEventPublisher
{
    Task PublishTripStartedAsync(Guid tripId, Guid vehicleId, Guid driverId);
    Task PublishTripCompletedAsync(Guid tripId, Guid vehicleId, Guid driverId, decimal distanceKm);
}