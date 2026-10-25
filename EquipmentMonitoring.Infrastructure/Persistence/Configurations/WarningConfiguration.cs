using EquipmentMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentMonitoring.Infrastructure.Persistence.Configurations
{
    public class WarningConfiguration : IEntityTypeConfiguration<Warning>
    {
        public void Configure(EntityTypeBuilder<Warning> builder)
        {
            builder.ToTable("Warnings", tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Warnings_MeasurementType_Allowed",
                    "MeasurementType IN ('Temperature', 'Voltage', 'Load')");
                tableBuilder.HasCheckConstraint(
                    "CK_Warnings_BreachType_Allowed",
                    "BreachType IN ('BelowMinimum', 'AboveMaximum')");
                tableBuilder.HasCheckConstraint(
                    "CK_Warnings_ThresholdScope_Allowed",
                    "ThresholdScope IN ('Global', 'Equipment')");
                tableBuilder.HasCheckConstraint(
                    "CK_Warnings_Status_Allowed",
                    "Status IN ('Active', 'Acknowledged')");
                tableBuilder.HasCheckConstraint(
                    "CK_Warnings_Acknowledgement_Consistent",
                    "(Status = 'Active' AND AcknowledgedAtUtc IS NULL) OR (Status = 'Acknowledged' AND AcknowledgedAtUtc IS NOT NULL)");
            });

            builder.HasKey(w => w.Id);

            builder.Property(w => w.MeasurementType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(32);

            builder.Property(w => w.BreachType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(32);

            builder.Property(w => w.ThresholdScope)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(32);

            builder.Property(w => w.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsConcurrencyToken();

            builder.Property(w => w.Unit)
                .IsRequired()
                .HasMaxLength(16);

            builder.Property(w => w.MeasuredAtUtc)
                .IsRequired()
                .HasConversion(
                    value => value,
                    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

            builder.Property(w => w.CreatedAtUtc)
                .IsRequired()
                .HasConversion(
                    value => value,
                    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

            builder.Property(w => w.AcknowledgedAtUtc)
                .HasConversion(
                    value => value,
                    value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);

            builder.HasOne(w => w.Equipment)
                .WithMany()
                .HasForeignKey(w => w.EquipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(w => w.Measurement)
                .WithMany()
                .HasForeignKey(w => w.MeasurementId)
                .OnDelete(DeleteBehavior.Cascade);

            // ThresholdId is a historical reference without a foreign key: deleting
            // a threshold must neither remove past warnings nor fail a concurrent write.
            builder.HasIndex(w => new { w.MeasurementId, w.ThresholdId })
                .IsUnique();

            builder.HasIndex(w => new { w.Status, w.CreatedAtUtc });
            builder.HasIndex(w => new { w.EquipmentId, w.Status, w.CreatedAtUtc });
        }
    }
}
