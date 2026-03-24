using MassTransit;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using Shared.Messages;

namespace NotificationService.Infrastructure.Messaging.Consumers;

/// Listens for DriverStatusChanged events.
/// When a driver becomes Available we check their license expiry.
/// Since the message doesn't carry expiry, we store a per-driver expiry
/// via a separate DriverLicenseExpiryMessage (published by Fleet on assignment).
/// For now we log the event and rely on DriverLicenseExpiryConsumer for the actual alert.
public class DriverStatusChangedConsumer : IConsumer<DriverStatusChangedMessage>
{
    private readonly ILogger<DriverStatusChangedConsumer> _logger;

    public DriverStatusChangedConsumer(ILogger<DriverStatusChangedConsumer> logger)
        => _logger = logger;

    public Task Consume(ConsumeContext<DriverStatusChangedMessage> context)
    {
        _logger.LogInformation(
            "NOTIFICATION-SVC received DriverStatusChanged | DriverId: {DriverId} | Status: {Status}",
            context.Message.DriverId, context.Message.NewStatus);
        return Task.CompletedTask;
    }
}