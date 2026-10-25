using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Tests.Infrastructure
{
    internal sealed class SqliteTestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private SqliteTestDatabase(SqliteConnection connection)
        {
            _connection = connection;
        }

        public static async Task<SqliteTestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var database = new SqliteTestDatabase(connection);
            await using var context = database.CreateContext();
            await context.Database.MigrateAsync();

            return database;
        }

        public EquipmentMonitoringDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<EquipmentMonitoringDbContext>()
                .UseSqlite(_connection)
                .Options;

            return new EquipmentMonitoringDbContext(options);
        }

        public async Task<Equipment> AddEquipmentAsync(string serialNumber, string name = "Compressor")
        {
            var equipment = new Equipment(name, "Compressor", serialNumber, "Workshop 1", EquipmentStatus.Working);

            await using var context = CreateContext();
            context.Equipment.Add(equipment);
            await context.SaveChangesAsync();

            return equipment;
        }

        public async Task<Measurement> AddMeasurementAsync(Guid equipmentId, MeasurementType type, double value, DateTime measuredAtUtc)
        {
            var measurement = new Measurement(equipmentId, type, value, type.GetUnit(), measuredAtUtc);

            await using var context = CreateContext();
            context.Measurements.Add(measurement);
            await context.SaveChangesAsync();

            return measurement;
        }

        public ValueTask DisposeAsync()
        {
            return _connection.DisposeAsync();
        }
    }
}
