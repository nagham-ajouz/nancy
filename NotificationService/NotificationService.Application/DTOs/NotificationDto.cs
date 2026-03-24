namespace NotificationService.Application.DTOs;
 
public record NotificationDto(
    Guid     Id,
    string   Type,
    string   Message,
    string   TargetRole,
    Guid?    TargetUserId,
    bool     IsRead,
    DateTime CreatedAt,
    Guid?    VehicleId,
    Guid?    DriverId,
    Guid?    TripId
);
 
public record MarkReadRequest(Guid NotificationId);
 
public record NotificationQueryRequest(
    string? Role       = null,
    Guid?   UserId     = null,
    bool?   UnreadOnly = null
);