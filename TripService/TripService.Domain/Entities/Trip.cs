using Shared.BaseClasses;
using Shared.Exceptions;
using Shared.ValueObjects;
using TripService.Domain.Enums;
using TripService.Domain.Events;
using TripService.Domain.Exceptions;

namespace TripService.Domain.Entities;

public class Trip : Entity
{
    public Guid      VehicleId     { get; private set; }
    public Guid      DriverId      { get; private set; }
    public Location  StartLocation { get; private set; }
    public Location  EndLocation   { get; private set; }
    public TripStatus Status       { get; private set; }
    public DateTime? StartTime     { get; private set; }
    public DateTime? EndTime       { get; private set; }
    public decimal?  DistanceKm    { get; private set; }
    public Money?    Cost          { get; private set; }

    private readonly List<TripLog> _logs = new();
    public IReadOnlyCollection<TripLog> Logs => _logs.AsReadOnly();

    private Trip() { }

    // Created in Requested state — no driver/vehicle yet
    public Trip(Guid id, Location startLocation, Location endLocation)
    {
        Id            = id;
        StartLocation = startLocation;
        EndLocation   = endLocation;
        Status        = TripStatus.Requested;
    }
    
    public void AssignResources(Guid vehicleId, Guid driverId)
    {
        if (Status != TripStatus.Requested)
            throw new InvalidTripStateTransitionException(Status, TripStatus.Assigned);

        VehicleId = vehicleId;
        DriverId  = driverId;
        Status    = TripStatus.Assigned;
    }

    public void Start()
    {
        if (Status != TripStatus.Assigned)
            throw new InvalidTripStateTransitionException(Status, TripStatus.InProgress);

        Status    = TripStatus.InProgress;
        StartTime = DateTime.UtcNow;

        AddDomainEvent(new TripStartedEvent(Id, VehicleId, DriverId));
    }

    public void Complete()
    {
        if (Status != TripStatus.InProgress)
            throw new InvalidTripStateTransitionException(Status, TripStatus.Completed);

        Status      = TripStatus.Completed;
        EndTime     = DateTime.UtcNow;
        DistanceKm  = CalculateDistance();

        AddDomainEvent(new TripCompletedEvent(Id, VehicleId, DriverId, DistanceKm ?? 0));
    }

    public void Invoice(Money cost)
    {
        if (Status != TripStatus.Completed)
            throw new InvalidTripStateTransitionException(Status, TripStatus.Invoiced);

        Cost   = cost;
        Status = TripStatus.Invoiced;
    }

    public TripLog AddLog(Guid logId, Location location, DateTime timestamp, decimal? speed = null)
    {
        if (Status != TripStatus.InProgress)
            throw new DomainException("Can only log location during an InProgress trip.");

        var log = new TripLog(logId, Id, location, timestamp, speed);
        _logs.Add(log);
        return log;
    }

    // Distance calculation (Haversine formula)
    private decimal? CalculateDistance()
    {
        if (_logs.Count < 2) 
            return 0;

        double total = 0;
        for (int i = 1; i < _logs.Count; i++)
        {
            total += Haversine(
                _logs[i - 1].Location.Latitude, _logs[i - 1].Location.Longitude,
                _logs[i].Location.Latitude,     _logs[i].Location.Longitude);
        }

        return (decimal)total;
    }

    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth radius in km
        double dLat = ToRad(lat2 - lat1);
        double dLon = ToRad(lon2 - lon1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                 + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                 * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}