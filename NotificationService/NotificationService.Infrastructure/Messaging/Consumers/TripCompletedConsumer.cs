using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using Shared.Messages;

namespace NotificationService.Infrastructure.Messaging.Consumers;

/// Generates a trip summary notification for the driver when a trip completes.
/// This is the fan-out pattern: Fleet also consumes TripCompleted (to update mileage),
/// and now Notification Service independently reacts to the same event.
public class TripCompletedConsumer : IConsumer<TripCompletedMessage>
{
    private readonly INotificationService _notifications;
    private readonly ILogger<TripCompletedConsumer> _logger;

    public TripCompletedConsumer(
        INotificationService notifications,
        ILogger<TripCompletedConsumer> logger)
    {
        _notifications = notifications;
        _logger        = logger;
    }

    public async Task Consume(ConsumeContext<TripCompletedMessage> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "NOTIFICATION-SVC received TripCompleted | TripId: {TripId} | DriverId: {DriverId} | Distance: {Distance}km",
            msg.TripId, msg.DriverId, msg.DistanceKm);

        // Trip summary for the driver who completed the trip
        await _notifications.CreateAsync(
            type:         "TripSummary",
            message:      $"Your trip {msg.TripId} has been completed. " +
                          $"Total distance: {msg.DistanceKm:F1} km. Well done!",
            targetRole:   "Driver",
            targetUserId: msg.DriverId,   // scoped to the specific driver
            driverId:     msg.DriverId,
            vehicleId:    msg.VehicleId,
            tripId:       msg.TripId
        );

        _logger.LogInformation(
            "Trip summary notification sent to driver {DriverId} for trip {TripId}",
            msg.DriverId, msg.TripId);
    }
}