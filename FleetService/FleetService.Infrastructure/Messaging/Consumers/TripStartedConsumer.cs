using FleetService.Application.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Messages;

namespace FleetService.Infrastructure.Messaging.Consumers;

// Marks the driver as OnTrip when Trip Service starts a trip
public class TripStartedConsumer : IConsumer<TripStartedMessage>
{
    private readonly IDriverRepository _driverRepository;
    private readonly ILogger<TripStartedConsumer> _logger;

    public TripStartedConsumer(IDriverRepository driverRepository, ILogger<TripStartedConsumer> logger)
    {
        _driverRepository = driverRepository;
        _logger           = logger;
    }

    public async Task Consume(ConsumeContext<TripStartedMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received TripStarted for driver {DriverId}", message.DriverId);

        var driver = await _driverRepository.GetByIdAsync(message.DriverId);
        if (driver is null)
        {
            _logger.LogWarning("Driver {DriverId} not found when processing TripStarted", message.DriverId);
            return;
        }

        driver.MarkOnTrip();
        await _driverRepository.UpdateAsync(driver);

        _logger.LogInformation("Driver {DriverId} marked as OnTrip", message.DriverId);
    }
}