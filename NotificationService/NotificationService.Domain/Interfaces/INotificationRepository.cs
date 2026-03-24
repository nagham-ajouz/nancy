using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id);
    Task<IEnumerable<Notification>> GetByRoleAsync(string role, bool? unreadOnly = null);
    Task<IEnumerable<Notification>> GetByUserAsync(Guid userId, bool? unreadOnly = null);
    Task AddAsync(Notification notification);
    Task UpdateAsync(Notification notification);
    Task<int> SaveChangesAsync();
}