namespace FleetService.Application.Interfaces;

public interface IFleetEventPublisher
{
    Task PublishVehicleStatusChangedAsync(Guid vehicleId, string newStatus);
    Task PublishDriverAssignedAsync(Guid vehicleId, Guid driverId);
    Task PublishDriverUnassignedAsync(Guid vehicleId, Guid driverId);
    Task PublishDriverStatusChangedAsync(Guid driverId, string newStatus);
}