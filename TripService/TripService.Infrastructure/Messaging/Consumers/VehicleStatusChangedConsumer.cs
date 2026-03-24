using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Messages;
using TripService.Application.Interfaces;

namespace TripService.Infrastructure.Messaging.Consumers;

// Updates local cache when Fleet changes a vehicle's status
public class VehicleStatusChangedConsumer : IConsumer<VehicleStatusChangedMessage>
{
    private readonly IVehicleAvailabilityCache _cache;
    private readonly ILogger<VehicleStatusChangedConsumer> _logger;

    public VehicleStatusChangedConsumer(IVehicleAvailabilityCache cache,
        ILogger<VehicleStatusChangedConsumer> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<VehicleStatusChangedMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received VehicleStatusChanged: {VehicleId} → {Status}",
            message.VehicleId, message.NewStatus);

        // Only Active vehicles are available for trips
        bool available = message.NewStatus == "Active";
        await _cache.SetVehicleAvailableAsync(message.VehicleId, available);
        await _cache.SetVehicleTypeAsync(message.VehicleId, message.VehicleType);
    }
}