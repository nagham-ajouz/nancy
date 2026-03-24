using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using Shared.Messages;

namespace NotificationService.Infrastructure.Messaging.Consumers;

/// When a driver is assigned to a vehicle, Fleet validates the license isn't expired.
/// But we want to warn 30 days BEFORE expiry. Since DriverAssignedMessage doesn't carry
/// the expiry date, we add a new message: DriverLicenseExpiryMessage published by Fleet
/// on assignment. This consumer handles that warning message.
/// 
/// NOTE: You need to add DriverLicenseExpiryMessage to Shared.Messages and publish it
/// from FleetService when a driver is assigned (see comment below).
public class DriverLicenseExpiryConsumer : IConsumer<DriverLicenseExpiryMessage>
{
    private readonly INotificationService _notifications;
    private readonly ILogger<DriverLicenseExpiryConsumer> _logger;

    public DriverLicenseExpiryConsumer(
        INotificationService notifications,
        ILogger<DriverLicenseExpiryConsumer> logger)
    {
        _notifications = notifications;
        _logger        = logger;
    }

    public async Task Consume(ConsumeContext<DriverLicenseExpiryMessage> context)
    {
        var msg = context.Message;
        var daysUntilExpiry = (msg.LicenseExpiry - DateTime.UtcNow).Days;

        _logger.LogInformation(
            "NOTIFICATION-SVC received DriverLicenseExpiry | DriverId: {DriverId} | Expiry: {Expiry} | DaysLeft: {Days}",
            msg.DriverId, msg.LicenseExpiry, daysUntilExpiry);

        if (daysUntilExpiry <= 30 && daysUntilExpiry >= 0)
        {
            await _notifications.CreateAsync(
                type:         "LicenseExpiring",
                message:      $"Driver {msg.DriverId}'s license expires in {daysUntilExpiry} day(s) " +
                              $"(on {msg.LicenseExpiry:yyyy-MM-dd}). Please arrange renewal.",
                targetRole:   "FleetManager",
                targetUserId: null,       // broadcast to all FleetManagers
                driverId:     msg.DriverId
            );

            _logger.LogWarning(
                "LICENSE WARNING: Driver {DriverId} license expires in {Days} days",
                msg.DriverId, daysUntilExpiry);
        }
    }
}