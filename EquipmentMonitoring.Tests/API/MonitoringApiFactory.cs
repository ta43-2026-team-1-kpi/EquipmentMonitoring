using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Infrastructure.Persistence;
using EquipmentMonitoring.Tests.TestDoubles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EquipmentMonitoring.Tests.API
{
    internal sealed class MonitoringApiFactory : WebApplicationFactory<Program>
    {
        public static readonly DateTime StartUtc = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        private MonitoringApiFactory()
        {
        }

        public FixedTimeProvider Time { get; } = new(StartUtc);

        public static async Task<MonitoringApiFactory> CreateAsync()
        {
            var factory = new MonitoringApiFactory();
            await factory._connection.OpenAsync();

            using var scope = factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<EquipmentMonitoringDbContext>().Database.MigrateAsync();

            return factory;
        }

        public async Task<T> QueryDbAsync<T>(Func<EquipmentMonitoringDbContext, Task<T>> query)
        {
            using var scope = Services.CreateScope();
            return await query(scope.ServiceProvider.GetRequiredService<EquipmentMonitoringDbContext>());
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<EquipmentMonitoringDbContext>>();
                services.RemoveAll<EquipmentMonitoringDbContext>();
                services.AddDbContext<EquipmentMonitoringDbContext>(options => options.UseSqlite(_connection));

                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Time);
            });
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    internal static class MonitoringApiClientExtensions
    {
        public static async Task<Guid> CreateEquipmentAsync(this HttpClient client, string serialNumber, string name = "Compressor K-1")
        {
            var response = await client.PostAsJsonAsync("/api/equipment", new
            {
                name,
                type = "Compressor",
                serialNumber,
                location = "Workshop 1",
                status = "Working"
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("id").GetGuid();
        }

        public static Task<HttpResponseMessage> PutGlobalThresholdAsync(this HttpClient client, MeasurementType type, double? min, double? max)
        {
            return client.PutAsJsonAsync($"/api/thresholds/{type}", new { minValue = min, maxValue = max });
        }

        public static Task<HttpResponseMessage> PutEquipmentThresholdAsync(this HttpClient client, Guid equipmentId, MeasurementType type, double? min, double? max)
        {
            return client.PutAsJsonAsync($"/api/equipment/{equipmentId}/thresholds/{type}", new { minValue = min, maxValue = max });
        }

        public static async Task<Guid> PostMeasurementAsync(this HttpClient client, Guid equipmentId, MeasurementType type, double value, DateTime measuredAtUtc)
        {
            var response = await client.PostAsJsonAsync($"/api/equipment/{equipmentId}/measurements", new
            {
                type = type.ToString(),
                value,
                unit = type.GetUnit(),
                measuredAt = measuredAtUtc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("id").GetGuid();
        }

        public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response)
        {
            var value = await response.Content.ReadFromJsonAsync<T>(MonitoringApiFactory.Json);
            Assert.NotNull(value);
            return value;
        }

        public static async Task<IReadOnlyDictionary<string, string[]>> ReadValidationErrorsAsync(this HttpResponseMessage response)
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("errors").Deserialize<Dictionary<string, string[]>>()!;
        }
    }
}
