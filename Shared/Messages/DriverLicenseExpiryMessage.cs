namespace Shared.Messages;

/// Published by Fleet Service when a driver is assigned to a vehicle.
/// Notification Service consumes this to warn if license expires within 30 days.
/// Add this file to your Shared/Messages folder and publish it from
/// FleetService.Infrastructure.Messaging.Publishers.FleetEventPublisher
/// inside PublishDriverAssignedAsync (you already have the driver's LicenseExpiry there).
public record DriverLicenseExpiryMessage(
    Guid     DriverId,
    DateTime LicenseExpiry
);