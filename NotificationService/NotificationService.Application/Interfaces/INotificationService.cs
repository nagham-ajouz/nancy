using NotificationService.Application.DTOs;

namespace NotificationService.Application.Interfaces;

public interface INotificationService
{
    Task CreateAsync(string type, string message, string targetRole,
        Guid? targetUserId = null, Guid? vehicleId = null,
        Guid? driverId = null, Guid? tripId = null);

    Task<IEnumerable<NotificationDto>> GetByRoleAsync(string role, bool? unreadOnly = null);
    Task<IEnumerable<NotificationDto>> GetByUserAsync(Guid userId, bool? unreadOnly = null);
    Task MarkAsReadAsync(Guid notificationId);
}