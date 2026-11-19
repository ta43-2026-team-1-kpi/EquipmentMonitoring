using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquipmentMonitoring.Infrastructure.Persistence
{
    public class PostgresDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PostgresEquipmentMonitoringDbContext>
    {
        public PostgresEquipmentMonitoringDbContext CreateDbContext(string[] args)
        {
            var host = Environment.GetEnvironmentVariable("LAB3_POSTGRES_HOST") ?? "localhost";
            var port = Environment.GetEnvironmentVariable("LAB3_POSTGRES_PORT") ?? "5432";
            var database = Environment.GetEnvironmentVariable("LAB3_POSTGRES_DB") ?? "equipment_monitoring";
            var username = Environment.GetEnvironmentVariable("LAB3_POSTGRES_USER") ?? "postgres";
            var password = Environment.GetEnvironmentVariable("LAB3_POSTGRES_PASSWORD") ?? "postgres";

            var optionsBuilder = new DbContextOptionsBuilder<PostgresEquipmentMonitoringDbContext>();
            optionsBuilder.UseNpgsql(
                $"Host={host};Port={port};Database={database};Username={username};Password={password}",
                npgsql => npgsql.MigrationsAssembly("EquipmentMonitoring.Infrastructure"));

            return new PostgresEquipmentMonitoringDbContext(optionsBuilder.Options);
        }
    }
}
