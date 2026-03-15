using Microsoft.EntityFrameworkCore;
using TripService.Domain.Entities;

namespace TripService.Infrastructure.Persistence;

public class TripDbContext : DbContext
{
    public DbSet<Trip>    Trips    { get; set; }
    public DbSet<TripLog> TripLogs { get; set; }

    public TripDbContext(DbContextOptions<TripDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up TripConfiguration and TripLogConfiguration automatically
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TripDbContext).Assembly);
    }
}