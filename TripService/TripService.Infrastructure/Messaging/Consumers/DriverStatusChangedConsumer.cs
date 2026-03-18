using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Messages;
using TripService.Application.Interfaces;

namespace TripService.Infrastructure.Messaging.Consumers;

// Updates local cache when Fleet changes a driver's status
public class DriverStatusChangedConsumer : IConsumer<DriverStatusChangedMessage>
{
    private readonly IVehicleAvailabilityCache _cache;
    private readonly ILogger<DriverStatusChangedConsumer> _logger;

    public DriverStatusChangedConsumer(IVehicleAvailabilityCache cache,
        ILogger<DriverStatusChangedConsumer> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DriverStatusChangedMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received DriverStatusChanged: {DriverId} → {Status}",
            message.DriverId, message.NewStatus);

        // Only Available drivers can be assigned to trips
        bool available = message.NewStatus == "Available";
        await _cache.SetDriverAvailableAsync(message.DriverId, available);
    }
}