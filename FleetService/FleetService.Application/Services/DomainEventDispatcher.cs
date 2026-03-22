using FleetService.Application.Interfaces;
using FleetService.Domain.Events;
using Shared.BaseClasses;

namespace FleetService.Application.Services;

// Reads domain events that entities raised and publishes them to RabbitMQ
// Called once after every save — no manual publisher calls needed anywhere
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IFleetEventPublisher _publisher;

    public DomainEventDispatcher(IFleetEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task DispatchAsync(Entity entity)
    {
        foreach (var domainEvent in entity.DomainEvents)
        {
            switch (domainEvent)
            {
                case VehicleStatusChangedEvent e:
                    await _publisher.PublishVehicleStatusChangedAsync(
                        e.VehicleId, e.NewStatus.ToString());
                    break;

                case DriverAssignedEvent e:
                    await _publisher.PublishDriverAssignedAsync(e.VehicleId, e.DriverId);
                    break;

                case DriverUnassignedEvent e:
                    await _publisher.PublishDriverUnassignedAsync(e.VehicleId, e.DriverId);
                    break;
            }
        }
        entity.ClearDomainEvents();
    }
}