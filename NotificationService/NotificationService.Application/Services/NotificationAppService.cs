using Microsoft.Extensions.Logging;
using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Exceptions;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Services;

public class NotificationAppService : INotificationService
{
    private readonly INotificationRepository _repo;
    private readonly ILogger<NotificationAppService> _logger;

    public NotificationAppService(INotificationRepository repo, ILogger<NotificationAppService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public async Task CreateAsync(string type, string message, string targetRole,
                                  Guid? targetUserId = null, Guid? vehicleId = null,
                                  Guid? driverId = null, Guid? tripId = null)
    {
        var notification = Notification.Create(type, message, targetRole,
                                               targetUserId, vehicleId, driverId, tripId);
        await _repo.AddAsync(notification);
        await _repo.SaveChangesAsync();

        _logger.LogInformation(
            "NOTIFICATION CREATED | Type: {Type} | Role: {Role} | Message: {Message}",
            type, targetRole, message);
    }

    public async Task<IEnumerable<NotificationDto>> GetByRoleAsync(string role, bool? unreadOnly = null)
    {
        var items = await _repo.GetByRoleAsync(role, unreadOnly);
        return items.Select(ToDto);
    }

    public async Task<IEnumerable<NotificationDto>> GetByUserAsync(Guid userId, bool? unreadOnly = null)
    {
        var items = await _repo.GetByUserAsync(userId, unreadOnly);
        return items.Select(ToDto);
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _repo.GetByIdAsync(notificationId)
                           ?? throw new NotificationNotFoundException(notificationId);
        notification.MarkAsRead();
        await _repo.UpdateAsync(notification);
        await _repo.SaveChangesAsync();
    }

    private static NotificationDto ToDto(Notification n) => new(
        n.Id, n.Type, n.Message, n.TargetRole,
        n.TargetUserId, n.IsRead, n.CreatedAt,
        n.VehicleId, n.DriverId, n.TripId);
}