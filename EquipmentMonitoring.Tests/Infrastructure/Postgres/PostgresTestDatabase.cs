using EquipmentMonitoring.Infrastructure.Persistence;

namespace EquipmentMonitoring.Tests.Infrastructure.Postgres
{
    internal sealed class PostgresTestDatabase : TestDatabase
    {
        private readonly PostgresServer _server;

        public PostgresTestDatabase(PostgresServer server, string name)
        {
            _server = server;
            Name = name;
        }

        public string Name { get; }

        public string ConnectionString => _server.GetConnectionString(Name);

        public override EquipmentMonitoringDbContext CreateContext()
        {
            return _server.CreateContext(Name);
        }

        public override async ValueTask DisposeAsync()
        {
            await _server.DropDatabaseAsync(Name);
        }
    }
}
