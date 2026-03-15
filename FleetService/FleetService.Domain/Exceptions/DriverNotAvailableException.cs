using Shared.Exceptions;

namespace FleetService.Domain.Exceptions;

public class DriverNotAvailableException : DomainException
{
    public DriverNotAvailableException(Guid driverId)
        : base($"Driver {driverId} is not available for assignment.") { }
}