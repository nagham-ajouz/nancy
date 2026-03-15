using FleetService.Domain.Entities;
using FleetService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetService.Infrastructure.Persistence.Configurations;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("drivers");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.FirstName)
            .IsRequired()
            .HasMaxLength(100); // → VARCHAR(100) in the database

        builder.Property(d => d.LastName)
            .IsRequired()
            .HasMaxLength(100);

        // Store LicenseNumber value object as a single column
        builder.Property(d => d.LicenseNumber)
            .HasConversion(
                l => l.Value, // save: LicenseNumber → string
                v => new LicenseNumber(v)) // load: string → LicenseNumber
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("license_number");

        builder.HasIndex(d => d.LicenseNumber)
            .IsUnique();

        builder.Property(d => d.LicenseExpiry)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(d => d.VehicleId)
            .IsRequired(false);
    }
}