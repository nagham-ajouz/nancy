// NotificationService.Infrastructure.Repositories.NotificationRepository.cs
using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;

    public NotificationRepository(NotificationDbContext context)
        => _context = context;

    public async Task<Notification?> GetByIdAsync(Guid id)
        => await _context.Notifications.FindAsync(id);

    public async Task<IEnumerable<Notification>> GetByRoleAsync(string role, bool? unreadOnly = null)
    {
        IQueryable<Notification> query = _context.Notifications
            .Where(n => n.TargetRole == role);

        if (unreadOnly == true)
            query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Notification>> GetByDriverIdAsync(Guid driverId, bool? unreadOnly = null)
    {
        IQueryable<Notification> query = _context.Notifications
            .Where(n => n.DriverId == driverId);

        if (unreadOnly == true)
            query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Notification notification)
        => await _context.Notifications.AddAsync(notification);

    public async Task UpdateAsync(Notification notification)
        => _context.Notifications.Update(notification);

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}