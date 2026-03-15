using FleetService.Domain.Enums;
using Shared.Exceptions;

namespace FleetService.Domain.Exceptions;

public class InvalidStateTransitionException : DomainException
{
    public InvalidStateTransitionException(VehicleStatus from, VehicleStatus to)
        : base($"Cannot transition vehicle from {from} to {to}.") { }
}