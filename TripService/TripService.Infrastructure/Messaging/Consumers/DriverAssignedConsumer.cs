using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Messages;
using TripService.Application.Interfaces;

namespace TripService.Infrastructure.Messaging.Consumers;

public class DriverAssignedConsumer : IConsumer<DriverAssignedMessage>
{
    private readonly IVehicleAvailabilityCache _cache;
    private readonly ILogger<DriverAssignedConsumer> _logger;

    public DriverAssignedConsumer(IVehicleAvailabilityCache cache,
        ILogger<DriverAssignedConsumer> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DriverAssignedMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received DriverAssigned: driver {DriverId} → vehicle {VehicleId}",
            message.DriverId, message.VehicleId);

        // Driver is now assigned — still available for trips until they go OnTrip
        await _cache.SetDriverAvailableAsync(message.DriverId, true);
    }
}