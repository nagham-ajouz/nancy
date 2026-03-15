using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripService.Domain.Entities;

namespace TripService.Infrastructure.Persistence.Configurations;

public class TripLogConfiguration : IEntityTypeConfiguration<TripLog>
{
    public void Configure(EntityTypeBuilder<TripLog> builder)
    {
        builder.ToTable("trip_logs");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TripId)
            .IsRequired();

        // Flatten Location value object into 2 columns (no address on TripLog)
        builder.OwnsOne(t => t.Location, loc =>
        {
            loc.Property(l => l.Latitude).HasColumnName("latitude").IsRequired();
            loc.Property(l => l.Longitude).HasColumnName("longitude").IsRequired();
            loc.Property(l => l.Address).HasColumnName("address").IsRequired();
        });

        builder.Property(t => t.Timestamp)
            .IsRequired();

        builder.Property(t => t.Speed)
            .HasPrecision(6, 2)
            .IsRequired(false);
    }
}