using TripService.Domain.Enums;
using Shared.Exceptions;

namespace TripService.Domain.Exceptions;

public class InvalidTripStateTransitionException : DomainException
{
    public InvalidTripStateTransitionException(TripStatus from, TripStatus to)
        : base($"Cannot transition trip from {from} to {to}.") { }
}