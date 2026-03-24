using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Persistence;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Type).IsRequired().HasMaxLength(100);
            e.Property(n => n.Message).IsRequired().HasMaxLength(1000);
            e.Property(n => n.TargetRole).IsRequired().HasMaxLength(50);
            e.HasIndex(n => n.TargetRole);
            e.HasIndex(n => n.TargetUserId);
            e.HasIndex(n => n.CreatedAt);
        });
    }
}