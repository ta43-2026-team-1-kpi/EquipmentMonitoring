using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence
{
    public class EquipmentMonitoringDbContext : DbContext
    {
        public EquipmentMonitoringDbContext(DbContextOptions<EquipmentMonitoringDbContext> options)
            : base(options)
        {
        }

        public DbSet<Equipment> Equipment => Set<Equipment>();

        public DbSet<Measurement> Measurements => Set<Measurement>();

        public DbSet<MaintenancePlan> MaintenancePlans => Set<MaintenancePlan>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new EquipmentConfiguration());
            modelBuilder.ApplyConfiguration(new MeasurementConfiguration());
            modelBuilder.ApplyConfiguration(new MaintenancePlanConfiguration());
        }
    }
}
