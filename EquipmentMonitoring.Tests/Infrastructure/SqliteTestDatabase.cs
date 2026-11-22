using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Tests.Infrastructure
{
    internal sealed class SqliteTestDatabase : TestDatabase
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

        public override EquipmentMonitoringDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<EquipmentMonitoringDbContext>()
                .UseSqlite(_connection)
                .Options;

            return new EquipmentMonitoringDbContext(options);
        }

        public override ValueTask DisposeAsync()
        {
            return _connection.DisposeAsync();
        }
    }
}
