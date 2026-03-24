namespace FleetService.Application.Interfaces;

public interface IFleetEventPublisher
{
    Task PublishVehicleStatusChangedAsync(Guid vehicleId, string newStatus, string vehicleType);
    Task PublishDriverAssignedAsync(Guid vehicleId, Guid driverId);
    Task PublishDriverUnassignedAsync(Guid vehicleId, Guid driverId);
    Task PublishDriverStatusChangedAsync(Guid driverId, string newStatus);
    Task PublishDriverLicenseExpiryAsync(Guid driverId, DateTime licenseExpiry);
}