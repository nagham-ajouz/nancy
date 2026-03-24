using FleetService.Application.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Messages;

namespace FleetService.Infrastructure.Messaging.Publishers;

// Called by application services after domain events fire
public class FleetEventPublisher : IFleetEventPublisher
{
    private readonly IDriverRepository _driverRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<FleetEventPublisher> _logger;

    public FleetEventPublisher(
        IPublishEndpoint publishEndpoint, 
        ILogger<FleetEventPublisher> logger,
        IDriverRepository driverRepository)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _driverRepository = driverRepository;
    }

    public async Task PublishVehicleStatusChangedAsync(Guid vehicleId, string newStatus, string vehicleType)
    {
        await _publishEndpoint.Publish(new VehicleStatusChangedMessage(vehicleId, newStatus, vehicleType));
        _logger.LogInformation(
            "PUBLISHED: VehicleStatusChanged | VehicleId: {VehicleId} | NewStatus: {Status}",
            vehicleId, newStatus);
    }

    public async Task PublishDriverAssignedAsync(Guid vehicleId, Guid driverId)
    {
        var driver = await _driverRepository.GetByIdAsync(driverId);
        
        if (driver is null)
            throw new NotFoundException($"Driver {driverId} not found.");
        
        await _publishEndpoint.Publish(new DriverAssignedMessage(vehicleId, driverId));
        await _publishEndpoint.Publish(new DriverLicenseExpiryMessage(driverId, driver.LicenseExpiry));
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
    
    public async Task PublishDriverLicenseExpiryAsync(Guid driverId, DateTime licenseExpiry)
    {
        await _publishEndpoint.Publish(new DriverLicenseExpiryMessage(driverId, licenseExpiry));
        _logger.LogInformation(
            "PUBLISHED: DriverLicenseExpiry | DriverId: {DriverId} | Expiry: {Expiry}",
            driverId, licenseExpiry);
    }
}