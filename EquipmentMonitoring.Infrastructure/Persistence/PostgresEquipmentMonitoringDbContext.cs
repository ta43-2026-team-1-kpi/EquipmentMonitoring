using EquipmentMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence
{
    public class PostgresEquipmentMonitoringDbContext : EquipmentMonitoringDbContext
    {
        public PostgresEquipmentMonitoringDbContext(DbContextOptions<PostgresEquipmentMonitoringDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Equipment>(builder =>
            {
                builder.ToTable("Equipment", tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint("CK_Equipment_Name_Length", "length(\"Name\") <= 200");
                    tableBuilder.HasCheckConstraint("CK_Equipment_Type_Length", "length(\"Type\") <= 100");
                    tableBuilder.HasCheckConstraint("CK_Equipment_SerialNumber_Length", "length(\"SerialNumber\") <= 100");
                    tableBuilder.HasCheckConstraint("CK_Equipment_Location_Length", "length(\"Location\") <= 200");
                    tableBuilder.HasCheckConstraint("CK_Equipment_Status_Allowed", "\"Status\" IN ('Working', 'Faulty', 'UnderMaintenance')");
                });

                builder.HasKey(e => e.Id);

                builder.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                builder.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(100);

                builder.Property(e => e.SerialNumber)
                    .IsRequired()
                    .HasMaxLength(100);

                builder.Property(e => e.Location)
                    .IsRequired()
                    .HasMaxLength(200);

                builder.Property(e => e.Status)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(32);

                builder.HasIndex(e => e.SerialNumber)
                    .IsUnique();
            });

            modelBuilder.Entity<Measurement>(builder =>
            {
                builder.ToTable("Measurements", tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_Measurements_Type_Allowed",
                        "\"Type\" IN ('Temperature', 'Voltage', 'Load')");
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
                    .HasColumnType("timestamp with time zone")
                    .HasConversion(
                        value => value,
                        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

                builder.HasOne(m => m.Equipment)
                    .WithMany(e => e.Measurements)
                    .HasForeignKey(m => m.EquipmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasIndex(m => new { m.EquipmentId, m.MeasuredAtUtc });
                builder.HasIndex(m => new { m.EquipmentId, m.Type, m.MeasuredAtUtc });
            });

            modelBuilder.Entity<MaintenancePlan>(builder =>
            {
                builder.ToTable("MaintenancePlans", tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_MaintenancePlans_WorkType_Length",
                        "length(\"WorkType\") <= 200");
                    tableBuilder.HasCheckConstraint(
                        "CK_MaintenancePlans_ResponsibleEmployee_Length",
                        "length(\"ResponsibleEmployee\") <= 200");
                    tableBuilder.HasCheckConstraint(
                        "CK_MaintenancePlans_Note_Length",
                        "\"Note\" IS NULL OR length(\"Note\") <= 1000");
                    tableBuilder.HasCheckConstraint(
                        "CK_MaintenancePlans_Status_Allowed",
                        "\"Status\" IN ('Planned', 'Completed', 'Cancelled')");
                });

                builder.HasKey(plan => plan.Id);

                builder.Property(plan => plan.PlannedDate)
                    .IsRequired()
                    .HasColumnType("date");

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
            });

            modelBuilder.Entity<Threshold>(builder =>
            {
                builder.ToTable("Thresholds", tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_Thresholds_MeasurementType_Allowed",
                        "\"MeasurementType\" IN ('Temperature', 'Voltage', 'Load')");
                    tableBuilder.HasCheckConstraint(
                        "CK_Thresholds_AtLeastOneLimit",
                        "\"MinValue\" IS NOT NULL OR \"MaxValue\" IS NOT NULL");
                    tableBuilder.HasCheckConstraint(
                        "CK_Thresholds_MinNotGreaterThanMax",
                        "\"MinValue\" IS NULL OR \"MaxValue\" IS NULL OR \"MinValue\" <= \"MaxValue\"");
                });

                builder.HasKey(t => t.Id);

                builder.Ignore(t => t.Scope);

                builder.Property(t => t.MeasurementType)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(32);

                builder.Property(t => t.CreatedAtUtc)
                    .IsRequired()
                    .HasColumnType("timestamp with time zone")
                    .HasConversion(
                        value => value,
                        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

                builder.Property(t => t.UpdatedAtUtc)
                    .IsRequired()
                    .HasColumnType("timestamp with time zone")
                    .HasConversion(
                        value => value,
                        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

                builder.HasOne(t => t.Equipment)
                    .WithMany()
                    .HasForeignKey(t => t.EquipmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasIndex(t => new { t.EquipmentId, t.MeasurementType })
                    .IsUnique()
                    .HasFilter("\"EquipmentId\" IS NOT NULL")
                    .HasDatabaseName("UX_Thresholds_EquipmentId_MeasurementType");

                builder.HasIndex(t => t.MeasurementType)
                    .IsUnique()
                    .HasFilter("\"EquipmentId\" IS NULL")
                    .HasDatabaseName("UX_Thresholds_Global_MeasurementType");
            });

            modelBuilder.Entity<Warning>(builder =>
            {
                builder.ToTable("Warnings", tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_Warnings_MeasurementType_Allowed",
                        "\"MeasurementType\" IN ('Temperature', 'Voltage', 'Load')");
                    tableBuilder.HasCheckConstraint(
                        "CK_Warnings_BreachType_Allowed",
                        "\"BreachType\" IN ('BelowMinimum', 'AboveMaximum')");
                    tableBuilder.HasCheckConstraint(
                        "CK_Warnings_ThresholdScope_Allowed",
                        "\"ThresholdScope\" IN ('Global', 'Equipment')");
                    tableBuilder.HasCheckConstraint(
                        "CK_Warnings_Status_Allowed",
                        "\"Status\" IN ('Active', 'Acknowledged')");
                    tableBuilder.HasCheckConstraint(
                        "CK_Warnings_Acknowledgement_Consistent",
                        "(\"Status\" = 'Active' AND \"AcknowledgedAtUtc\" IS NULL) OR (\"Status\" = 'Acknowledged' AND \"AcknowledgedAtUtc\" IS NOT NULL)");
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
                    .HasColumnType("timestamp with time zone")
                    .HasConversion(
                        value => value,
                        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

                builder.Property(w => w.CreatedAtUtc)
                    .IsRequired()
                    .HasColumnType("timestamp with time zone")
                    .HasConversion(
                        value => value,
                        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

                builder.Property(w => w.AcknowledgedAtUtc)
                    .HasColumnType("timestamp with time zone")
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

                builder.HasIndex(w => new { w.MeasurementId, w.ThresholdId })
                    .IsUnique();

                builder.HasIndex(w => new { w.Status, w.CreatedAtUtc });
                builder.HasIndex(w => new { w.EquipmentId, w.Status, w.CreatedAtUtc });
            });
        }
    }
}
