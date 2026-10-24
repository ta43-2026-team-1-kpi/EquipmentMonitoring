using System.Net;
using System.Text;
using System.Text.Json;
using EquipmentMonitoring.Application.DTOs.Equipment;
using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EquipmentMonitoring.Tests.API
{
    public class MeasurementsHttpIntegrationTests
    {
        [Fact]
        public async Task PostMeasurement_WithValidPayload_ShouldReturn201AndEnumAsString()
        {
            await using var factory = new CustomApiFactory();
            using var client = factory.CreateClient();

            var equipmentId = await CreateEquipmentAsync(client, "SN-HTTP-001");

            const string body = """
                                {
                                  "type": "Temperature",
                                  "value": -5.5,
                                  "unit": "°C",
                                  "measuredAt": "2026-10-24T12:30:00+02:00"
                                }
                                """;

            var response = await client.PostAsync(
                $"/api/equipment/{equipmentId}/measurements",
                new StringContent(body, Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(content);
            Assert.Equal("Temperature", json.RootElement.GetProperty("type").GetString());
            Assert.Equal("2026-10-24T10:30:00+00:00", json.RootElement.GetProperty("measuredAtUtc").GetString());
        }

        [Fact]
        public async Task PostMeasurement_WithDateWithoutOffset_ShouldReturn400()
        {
            await using var factory = new CustomApiFactory();
            using var client = factory.CreateClient();

            var equipmentId = await CreateEquipmentAsync(client, "SN-HTTP-002");

            const string body = """
                                {
                                  "type": "Voltage",
                                  "value": 220,
                                  "unit": "V",
                                  "measuredAt": "2026-10-24T10:30:00"
                                }
                                """;

            var response = await client.PostAsync(
                $"/api/equipment/{equipmentId}/measurements",
                new StringContent(body, Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostMeasurement_WithNonNumericJsonValue_ShouldReturn400()
        {
            await using var factory = new CustomApiFactory();
            using var client = factory.CreateClient();

            var equipmentId = await CreateEquipmentAsync(client, "SN-HTTP-003");

            const string body = """
                                {
                                  "type": "Voltage",
                                  "value": "abc",
                                  "unit": "V",
                                  "measuredAt": "2026-10-24T10:30:00Z"
                                }
                                """;

            var response = await client.PostAsync(
                $"/api/equipment/{equipmentId}/measurements",
                new StringContent(body, Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetHistory_WithTypeAndRange_ShouldReturnFilteredMeasurements()
        {
            await using var factory = new CustomApiFactory();
            using var client = factory.CreateClient();

            var equipmentId = await CreateEquipmentAsync(client, "SN-HTTP-004");

            await PostMeasurementAsync(client, equipmentId, "Temperature", 10, "°C", "2026-10-24T10:00:00Z");
            await PostMeasurementAsync(client, equipmentId, "Voltage", 210, "V", "2026-10-24T11:00:00Z");
            await PostMeasurementAsync(client, equipmentId, "Voltage", 220, "V", "2026-10-24T12:00:00Z");

            var response = await client.GetAsync(
                $"/api/equipment/{equipmentId}/measurements?type=voltage&from=2026-10-24T11:00:00Z&to=2026-10-24T12:00:00Z");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(content);
            Assert.Equal(2, json.RootElement.GetArrayLength());
            Assert.Equal("Voltage", json.RootElement[0].GetProperty("type").GetString());
            Assert.Equal("Voltage", json.RootElement[1].GetProperty("type").GetString());
        }

        [Fact]
        public async Task GetHistory_WithFromGreaterThanTo_ShouldReturn400()
        {
            await using var factory = new CustomApiFactory();
            using var client = factory.CreateClient();

            var equipmentId = await CreateEquipmentAsync(client, "SN-HTTP-005");

            var response = await client.GetAsync(
                $"/api/equipment/{equipmentId}/measurements?from=2026-10-24T12:00:00Z&to=2026-10-24T11:00:00Z");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetHistory_ForMissingEquipment_ShouldReturn404()
        {
            await using var factory = new CustomApiFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync($"/api/equipment/{Guid.NewGuid()}/measurements");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static async Task<Guid> CreateEquipmentAsync(HttpClient client, string serialNumber)
        {
            var uniqueSerialNumber = $"{serialNumber}-{Guid.NewGuid():N}";

            var body = $$"""
                         {
                           "name": "Http Equipment",
                           "type": "Compressor",
                           "serialNumber": "{{uniqueSerialNumber}}",
                           "location": "Test Lab",
                           "status": "Working"
                         }
                         """;

            var response = await client.PostAsync(
                "/api/equipment",
                new StringContent(body, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("id").GetGuid();
        }

        private static async Task PostMeasurementAsync(
            HttpClient client,
            Guid equipmentId,
            string type,
            double value,
            string unit,
            string measuredAt)
        {
            var body = $$"""
                         {
                           "type": "{{type}}",
                           "value": {{value}},
                           "unit": "{{unit}}",
                           "measuredAt": "{{measuredAt}}"
                         }
                         """;

            var response = await client.PostAsync(
                $"/api/equipment/{equipmentId}/measurements",
                new StringContent(body, Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();
        }

        private sealed class CustomApiFactory : WebApplicationFactory<Program>
        {
            private readonly SqliteConnection _connection = new("Data Source=:memory:");

            protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
            {
                builder.ConfigureServices(services =>
                {
                    if (_connection.State != System.Data.ConnectionState.Open)
                    {
                        _connection.Open();
                    }

                    services.RemoveAll<DbContextOptions<EquipmentMonitoringDbContext>>();
                    services.RemoveAll<EquipmentMonitoringDbContext>();

                    services.AddDbContext<EquipmentMonitoringDbContext>(options =>
                        options.UseSqlite(_connection));

                    using var provider = services.BuildServiceProvider();
                    using var scope = provider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<EquipmentMonitoringDbContext>();
                    context.Database.Migrate();
                });
            }

            public override async ValueTask DisposeAsync()
            {
                await base.DisposeAsync();
                await _connection.DisposeAsync();
            }
        }
    }
}
