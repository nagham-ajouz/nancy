using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _db;

    public NotificationRepository(NotificationDbContext db) => _db = db;

    public async Task<Notification?> GetByIdAsync(Guid id) =>
        await _db.Notifications.FindAsync(id);

    public async Task<IEnumerable<Notification>> GetByRoleAsync(string role, bool? unreadOnly = null)
    {
        var query = _db.Notifications
            .Where(n => n.TargetRole == role);

        if (unreadOnly == true)
            query = query.Where(n => !n.IsRead);

        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
    }

    public async Task<IEnumerable<Notification>> GetByUserAsync(Guid userId, bool? unreadOnly = null)
    {
        var query = _db.Notifications
            .Where(n => n.TargetUserId == userId);

        if (unreadOnly == true)
            query = query.Where(n => !n.IsRead);

        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
    }

    public async Task AddAsync(Notification notification) =>
        await _db.Notifications.AddAsync(notification);

    public Task UpdateAsync(Notification notification)
    {
        _db.Notifications.Update(notification);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}