using EquipmentMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentMonitoring.Infrastructure.Persistence.Configurations
{
    public class MaintenancePlanConfiguration : IEntityTypeConfiguration<MaintenancePlan>
    {
        public void Configure(EntityTypeBuilder<MaintenancePlan> builder)
        {
            builder.ToTable("MaintenancePlans", tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_MaintenancePlans_WorkType_Length",
                    "length(WorkType) <= 200");
                tableBuilder.HasCheckConstraint(
                    "CK_MaintenancePlans_ResponsibleEmployee_Length",
                    "length(ResponsibleEmployee) <= 200");
                tableBuilder.HasCheckConstraint(
                    "CK_MaintenancePlans_Note_Length",
                    "Note IS NULL OR length(Note) <= 1000");
                tableBuilder.HasCheckConstraint(
                    "CK_MaintenancePlans_Status_Allowed",
                    "Status IN ('Planned', 'Completed', 'Cancelled')");
            });

            builder.HasKey(plan => plan.Id);

            builder.Property(plan => plan.PlannedDate)
                .IsRequired();

            builder.Property(plan => plan.WorkType)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(plan => plan.ResponsibleEmployee)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(plan => plan.Note)
                .HasMaxLength(1000);

            builder.Property(plan => plan.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(32);

            builder.HasOne(plan => plan.Equipment)
                .WithMany(equipment => equipment.MaintenancePlans)
                .HasForeignKey(plan => plan.EquipmentId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasIndex(plan => new { plan.PlannedDate, plan.Id });
            builder.HasIndex(plan => new { plan.EquipmentId, plan.PlannedDate });
        }
    }
}
