using EquipmentMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentMonitoring.Infrastructure.Persistence.Configurations
{
    public class MeasurementConfiguration : IEntityTypeConfiguration<Measurement>
    {
        public void Configure(EntityTypeBuilder<Measurement> builder)
        {
            builder.ToTable("Measurements", tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Measurements_Type_Allowed",
                    "Type IN ('Temperature', 'Voltage', 'Load')");
            });

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Type)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(32);

            builder.Property(m => m.Value)
                .IsRequired();

            builder.Property(m => m.Unit)
                .IsRequired()
                .HasMaxLength(16);

            builder.Property(m => m.MeasuredAtUtc)
                .IsRequired()
                .HasConversion(
                    value => value,
                    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

            builder.HasOne(m => m.Equipment)
                .WithMany(e => e.Measurements)
                .HasForeignKey(m => m.EquipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(m => new { m.EquipmentId, m.MeasuredAtUtc });
            builder.HasIndex(m => new { m.EquipmentId, m.Type, m.MeasuredAtUtc });
        }
    }
}
