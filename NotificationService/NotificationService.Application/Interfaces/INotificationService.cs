// NotificationService.Application.Interfaces.INotificationService.cs

using NotificationService.Application.DTOs;

namespace NotificationService.Application.Interfaces;

public interface INotificationService
{
    Task CreateAsync(string type, string message, string targetRole,
        Guid? targetUserId = null, Guid? vehicleId = null,
        Guid? driverId = null, Guid? tripId = null);
    
    Task<NotificationDto?> GetByIdAsync(Guid id); // ← ADD THIS
    Task<IEnumerable<NotificationDto>> GetByRoleAsync(string role, bool? unreadOnly = null);
    Task<IEnumerable<NotificationDto>> GetByDriverIdAsync(Guid driverId, bool? unreadOnly = null);
    Task MarkAsReadAsync(Guid notificationId);
}