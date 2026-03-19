using Shared.BaseClasses;
using TripService.Application.Interfaces;
using TripService.Domain.Events;

namespace TripService.Application.Services;

public class DomainEventDispatcher
{
    private readonly ITripEventPublisher _publisher;

    public DomainEventDispatcher(ITripEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task DispatchAsync(Entity entity)
    {
        foreach (var domainEvent in entity.DomainEvents)
        {
            switch (domainEvent)
            {
                case TripStartedEvent e:
                    await _publisher.PublishTripStartedAsync(e.TripId, e.VehicleId, e.DriverId);
                    break;

                case TripCompletedEvent e:
                    await _publisher.PublishTripCompletedAsync(
                        e.TripId, e.VehicleId, e.DriverId, e.DistanceKm);
                    break;
            }
        }
        entity.ClearDomainEvents();
    }
}