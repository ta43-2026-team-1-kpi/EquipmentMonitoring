using EquipmentMonitoring.Tests.Infrastructure.Postgres;
using EquipmentMonitoring.Tests.TestDoubles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EquipmentMonitoring.Tests.API.Postgres
{
    /// <summary>
    /// Hosts the API the way the container runs it: "Docker" environment, Npgsql with the PostgreSQL model,
    /// migrations applied on startup to an empty database. Only the connection string and the clock are replaced.
    /// </summary>
    internal sealed class PostgresApiFactory : WebApplicationFactory<Program>
    {
        private readonly PostgresTestDatabase _database;

        private PostgresApiFactory(PostgresTestDatabase database)
        {
            _database = database;
        }

        public FixedTimeProvider Time { get; } = new(MonitoringApiFactory.StartUtc);

        public static async Task<PostgresApiFactory> CreateAsync(PostgresServer server)
        {
            return new PostgresApiFactory(await server.CreateEmptyDatabaseAsync());
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Docker");
            builder.UseSetting("ConnectionStrings:EquipmentDatabase", _database.ConnectionString);

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Time);
            });
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            await _database.DisposeAsync();
        }
    }
}
