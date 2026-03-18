using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Messages;
using TripService.Application.Interfaces;

namespace TripService.Infrastructure.Messaging.Consumers;

public class DriverUnassignedConsumer : IConsumer<DriverUnassignedMessage>
{
    private readonly IVehicleAvailabilityCache _cache;
    private readonly ILogger<DriverUnassignedConsumer> _logger;

    public DriverUnassignedConsumer(IVehicleAvailabilityCache cache,
        ILogger<DriverUnassignedConsumer> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DriverUnassignedMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received DriverUnassigned: driver {DriverId} from vehicle {VehicleId}",
            message.DriverId, message.VehicleId);

        await _cache.SetDriverAvailableAsync(message.DriverId, true);
    }
}