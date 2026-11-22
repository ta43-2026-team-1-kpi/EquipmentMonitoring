using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace EquipmentMonitoring.Tests.Infrastructure.Postgres
{
    /// <summary>
    /// One PostgreSQL container per test run. Migrations are applied once to a template database;
    /// every test gets its own copy of it, so tests stay isolated without re-running migrations.
    /// </summary>
    public sealed class PostgresServer : IAsyncLifetime
    {
        private const string TemplateDatabase = "equipment_monitoring_template";

        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(ComposeFile.PostgresImage)
            .WithCommand("-c", "fsync=off", "-c", "synchronous_commit=off", "-c", "full_page_writes=off")
            .Build();

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
            await ExecuteAsync($"CREATE DATABASE \"{TemplateDatabase}\"");

            await using (var context = CreateContext(TemplateDatabase))
            {
                await context.Database.MigrateAsync();
            }

            // A database cannot be used as a template while it has open connections.
            NpgsqlConnection.ClearAllPools();
        }

        internal Task<PostgresTestDatabase> CreateDatabaseAsync()
        {
            return CreateDatabaseAsync($"TEMPLATE \"{TemplateDatabase}\"");
        }

        internal Task<PostgresTestDatabase> CreateEmptyDatabaseAsync()
        {
            return CreateDatabaseAsync(string.Empty);
        }

        internal string GetConnectionString(string database)
        {
            return new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = database }.ConnectionString;
        }

        internal PostgresEquipmentMonitoringDbContext CreateContext(string database)
        {
            var options = new DbContextOptionsBuilder<PostgresEquipmentMonitoringDbContext>()
                .UseNpgsql(GetConnectionString(database))
                .Options;

            return new PostgresEquipmentMonitoringDbContext(options);
        }

        internal async Task DropDatabaseAsync(string database)
        {
            await using (var connection = new NpgsqlConnection(GetConnectionString(database)))
            {
                NpgsqlConnection.ClearPool(connection);
            }

            await ExecuteAsync($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)");
        }

        public Task DisposeAsync()
        {
            return _container.DisposeAsync().AsTask();
        }

        private async Task<PostgresTestDatabase> CreateDatabaseAsync(string options)
        {
            var database = $"test_{Guid.NewGuid():N}";
            await ExecuteAsync($"CREATE DATABASE \"{database}\" {options}");

            return new PostgresTestDatabase(this, database);
        }

        private async Task ExecuteAsync(string sql)
        {
            await using var connection = new NpgsqlConnection(_container.GetConnectionString());
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    [CollectionDefinition(Name)]
    public sealed class PostgresCollection : ICollectionFixture<PostgresServer>
    {
        public const string Name = "PostgreSQL";
    }
}
