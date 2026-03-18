using Shared.BaseClasses;
using Shared.ValueObjects;

namespace TripService.Domain.Entities;

public class TripLog : Entity
{
    public Guid     TripId    { get; private set; }
    public Location Location  { get; private set; }
    public DateTime Timestamp { get; private set; }
    public decimal? Speed     { get; private set; }

    private TripLog()
    {
        Location = null!;
    }

    public TripLog(Guid id, Guid tripId, Location location, DateTime timestamp, decimal? speed = null)
    {
        if (speed < 0) 
            throw new ArgumentException("Speed cannot be negative.");

        Id        = id;
        TripId    = tripId;
        Location  = location;
        Timestamp = timestamp;
        Speed     = speed;
    }
}