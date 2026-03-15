using Shared.Exceptions;

namespace FleetService.Domain.Exceptions;

public class LicenseExpiredException : DomainException
{
    public LicenseExpiredException(Guid driverId)
        : base($"Driver {driverId} has an expired license.") { }
}