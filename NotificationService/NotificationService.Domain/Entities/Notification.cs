using Shared.BaseClasses;

namespace NotificationService.Domain.Entities;

public class Notification : Entity
{
    public string Type        { get; private set; } = string.Empty; // "LicenseExpiring", "MileageAlert", "TripSummary"
    public string Message     { get; private set; } = string.Empty;
    public string TargetRole  { get; private set; } = string.Empty; // "Driver", "FleetManager", "Admin"
    public Guid?  TargetUserId { get; private set; }                // null = broadcast to role
    public bool   IsRead      { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Contextual references
    public Guid?  VehicleId { get; private set; }
    public Guid?  DriverId  { get; private set; }
    public Guid?  TripId    { get; private set; }

    private Notification() { }

    public static Notification Create(
        string type,
        string message,
        string targetRole,
        Guid?  targetUserId = null,
        Guid?  vehicleId    = null,
        Guid?  driverId     = null,
        Guid?  tripId       = null)
    {
        return new Notification
        {
            Id           = Guid.NewGuid(),
            Type         = type,
            Message      = message,
            TargetRole   = targetRole,
            TargetUserId = targetUserId,
            IsRead       = false,
            CreatedAt    = DateTime.UtcNow,
            VehicleId    = vehicleId,
            DriverId     = driverId,
            TripId       = tripId
        };
    }

    public void MarkAsRead() => IsRead = true;
}