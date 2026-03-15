using FleetService.Domain.Entities;
using FleetService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetService.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");

        builder.HasKey(v => v.Id);

        // Store PlateNumber value object as a single column
        builder.Property(v => v.PlateNumber)
            .HasConversion(
                p => p.Value,                    // save: PlateNumber → string
                v => new PlateNumber(v))          // load: string → PlateNumber
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("plate_number");

        builder.HasIndex(v => v.PlateNumber)
            .IsUnique();

        builder.Property(v => v.Model)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Year)
            .IsRequired();

        // Store enum as string ("Active") instead of int (1) — easier to read in DB
        builder.Property(v => v.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(v => v.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(v => v.Mileage)
            .HasPrecision(10, 2);

        builder.Property(v => v.DriverId)
            .IsRequired(false);
    }
}