using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using Shared.Messages;

namespace NotificationService.Infrastructure.Messaging.Consumers;

/// Triggers a FleetManager alert when a vehicle enters InMaintenance state,
/// which in your domain is the signal that mileage threshold was hit or
/// maintenance was explicitly requested.
public class VehicleStatusChangedConsumer : IConsumer<VehicleStatusChangedMessage>
{
    private readonly INotificationService _notifications;
    private readonly ILogger<VehicleStatusChangedConsumer> _logger;

    public VehicleStatusChangedConsumer(
        INotificationService notifications,
        ILogger<VehicleStatusChangedConsumer> logger)
    {
        _notifications = notifications;
        _logger        = logger;
    }

    public async Task Consume(ConsumeContext<VehicleStatusChangedMessage> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "NOTIFICATION-SVC received VehicleStatusChanged | VehicleId: {VehicleId} | Status: {Status}",
            msg.VehicleId, msg.NewStatus);

        // Mileage threshold alert: Fleet transitions vehicle → InMaintenance when threshold hit
        if (msg.NewStatus == "InMaintenance")
        {
            await _notifications.CreateAsync(
                type:       "MileageAlert",
                message:    $"Vehicle {msg.VehicleId} ({msg.VehicleType}) has entered maintenance. " +
                            $"Mileage threshold may have been reached — inspection required.",
                targetRole: "FleetManager",
                vehicleId:  msg.VehicleId
            );

            _logger.LogWarning(
                "ALERT: Vehicle {VehicleId} entered InMaintenance — mileage alert sent to FleetManagers",
                msg.VehicleId);
        }

        // Also notify Admin when a vehicle is decommissioned
        if (msg.NewStatus == "Decommissioned")
        {
            await _notifications.CreateAsync(
                type:       "VehicleDecommissioned",
                message:    $"Vehicle {msg.VehicleId} ({msg.VehicleType}) has been decommissioned.",
                targetRole: "Admin",
                vehicleId:  msg.VehicleId
            );
        }
    }
}