using EquipmentMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentMonitoring.Infrastructure.Persistence.Configurations
{
    public class ThresholdConfiguration : IEntityTypeConfiguration<Threshold>
    {
        private const string EquipmentUniqueIndexName = "UX_Thresholds_EquipmentId_MeasurementType";
        private const string GlobalUniqueIndexName = "UX_Thresholds_Global_MeasurementType";

        public void Configure(EntityTypeBuilder<Threshold> builder)
        {
            builder.ToTable("Thresholds", tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Thresholds_MeasurementType_Allowed",
                    "MeasurementType IN ('Temperature', 'Voltage', 'Load')");
                tableBuilder.HasCheckConstraint(
                    "CK_Thresholds_AtLeastOneLimit",
                    "MinValue IS NOT NULL OR MaxValue IS NOT NULL");
                tableBuilder.HasCheckConstraint(
                    "CK_Thresholds_MinNotGreaterThanMax",
                    "MinValue IS NULL OR MaxValue IS NULL OR MinValue <= MaxValue");
            });

            builder.HasKey(t => t.Id);

            builder.Ignore(t => t.Scope);

            builder.Property(t => t.MeasurementType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(32);

            builder.Property(t => t.CreatedAtUtc)
                .IsRequired()
                .HasConversion(
                    value => value,
                    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

            builder.Property(t => t.UpdatedAtUtc)
                .IsRequired()
                .HasConversion(
                    value => value,
                    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

            builder.HasOne(t => t.Equipment)
                .WithMany()
                .HasForeignKey(t => t.EquipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // A NULL EquipmentId never collides in a unique index, so global thresholds
            // need their own filtered index to stay one per measurement type.
            builder.HasIndex(t => new { t.EquipmentId, t.MeasurementType })
                .IsUnique()
                .HasFilter("EquipmentId IS NOT NULL")
                .HasDatabaseName(EquipmentUniqueIndexName);

            builder.HasIndex(t => t.MeasurementType)
                .IsUnique()
                .HasFilter("EquipmentId IS NULL")
                .HasDatabaseName(GlobalUniqueIndexName);
        }
    }
}
