using FleetService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FleetService.Infrastructure.Persistence;

public class FleetDbContext : DbContext
{
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Driver>  Drivers  { get; set; }

    public FleetDbContext(DbContextOptions<FleetDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FleetDbContext).Assembly);
    }
}