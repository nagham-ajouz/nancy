using FleetService.Application.Interfaces;
using MassTransit;
using Shared.Messages;

namespace FleetService.Infrastructure.Messaging.Publishers;

// Called by application services after domain events fire
public class FleetEventPublisher : IFleetEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public FleetEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishVehicleStatusChangedAsync(Guid vehicleId, string newStatus)
    {
        await _publishEndpoint.Publish(new VehicleStatusChangedMessage(vehicleId, newStatus));
    }

    public async Task PublishDriverAssignedAsync(Guid vehicleId, Guid driverId)
    {
        await _publishEndpoint.Publish(new DriverAssignedMessage(vehicleId, driverId));
    }

    public async Task PublishDriverUnassignedAsync(Guid vehicleId, Guid driverId)
    {
        await _publishEndpoint.Publish(new DriverUnassignedMessage(vehicleId, driverId));
    }

    public async Task PublishDriverStatusChangedAsync(Guid driverId, string newStatus)
    {
        await _publishEndpoint.Publish(new DriverStatusChangedMessage(driverId, newStatus));
    }
}