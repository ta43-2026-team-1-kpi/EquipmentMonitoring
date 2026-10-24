using EquipmentMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentMonitoring.Infrastructure.Persistence.Configurations
{
    public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
    {
        public void Configure(EntityTypeBuilder<Equipment> builder)
        {
            builder.ToTable("Equipment", tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("CK_Equipment_Name_Length", "length(Name) <= 200");
                tableBuilder.HasCheckConstraint("CK_Equipment_Type_Length", "length(Type) <= 100");
                tableBuilder.HasCheckConstraint("CK_Equipment_SerialNumber_Length", "length(SerialNumber) <= 100");
                tableBuilder.HasCheckConstraint("CK_Equipment_Location_Length", "length(Location) <= 200");
                tableBuilder.HasCheckConstraint("CK_Equipment_Status_Allowed", "Status IN ('Working', 'Faulty', 'UnderMaintenance')");
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
        }
    }
}
