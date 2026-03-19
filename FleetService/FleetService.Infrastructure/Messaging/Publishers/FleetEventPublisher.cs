using FleetService.Application.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Messages;

namespace FleetService.Infrastructure.Messaging.Publishers;

// Called by application services after domain events fire
public class FleetEventPublisher : IFleetEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<FleetEventPublisher> _logger;

    public FleetEventPublisher(IPublishEndpoint publishEndpoint, ILogger<FleetEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishVehicleStatusChangedAsync(Guid vehicleId, string newStatus)
    {
        await _publishEndpoint.Publish(new VehicleStatusChangedMessage(vehicleId, newStatus));
        _logger.LogInformation(
            "PUBLISHED: VehicleStatusChanged | VehicleId: {VehicleId} | NewStatus: {Status}",
            vehicleId, newStatus);
    }

    public async Task PublishDriverAssignedAsync(Guid vehicleId, Guid driverId)
    {
        await _publishEndpoint.Publish(new DriverAssignedMessage(vehicleId, driverId));
        _logger.LogInformation(
            "PUBLISHED: DriverAssigned | VehicleId: {VehicleId} | DriverId: {DriverId}",
            vehicleId, driverId);
    }

    public async Task PublishDriverUnassignedAsync(Guid vehicleId, Guid driverId)
    {
        await _publishEndpoint.Publish(new DriverUnassignedMessage(vehicleId, driverId));
        _logger.LogInformation(
            "PUBLISHED: DriverUnassigned | VehicleId: {VehicleId} | DriverId: {DriverId}",
            vehicleId, driverId);
    }

    public async Task PublishDriverStatusChangedAsync(Guid driverId, string newStatus)
    {
        await _publishEndpoint.Publish(new DriverStatusChangedMessage(driverId, newStatus));
        _logger.LogInformation(
            "PUBLISHED: DriverStatusChanged | DriverId: {DriverId} | NewStatus: {Status}",
            driverId, newStatus);
    }
}