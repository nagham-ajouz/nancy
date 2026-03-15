using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripService.Domain.Entities;

namespace TripService.Infrastructure.Persistence.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("trips");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.VehicleId)
               .IsRequired();

        builder.Property(t => t.DriverId)
               .IsRequired();

        // Flatten StartLocation into 3 columns
        builder.OwnsOne(t => t.StartLocation, loc =>
        {
            loc.Property(l => l.Latitude).HasColumnName("start_lat").IsRequired();
            loc.Property(l => l.Longitude).HasColumnName("start_lng").IsRequired();
            loc.Property(l => l.Address).HasColumnName("start_address").IsRequired();
        });

        // Flatten EndLocation into 3 columns
        builder.OwnsOne(t => t.EndLocation, loc =>
        {
            loc.Property(l => l.Latitude).HasColumnName("end_lat").IsRequired();
            loc.Property(l => l.Longitude).HasColumnName("end_lng").IsRequired();
            loc.Property(l => l.Address).HasColumnName("end_address").IsRequired();
        });

        builder.Property(t => t.Status)
               .HasConversion<string>()
               .IsRequired();

        builder.Property(t => t.StartTime)
               .IsRequired(false);

        builder.Property(t => t.EndTime)
               .IsRequired(false);

        builder.Property(t => t.DistanceKm)
               .HasPrecision(10, 2)
               .IsRequired(false);

        // Flatten Money value object into 2 columns
        builder.OwnsOne(t => t.Cost, money =>
        {
            money.Property(m => m.Amount).HasColumnName("cost_amount").HasPrecision(10, 2);
            money.Property(m => m.Currency).HasColumnName("cost_currency").HasMaxLength(3);
        });

        // Trip owns its logs — deleting a trip deletes its logs
        builder.HasMany(t => t.Logs)
               .WithOne()
               .HasForeignKey(l => l.TripId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}