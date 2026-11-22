using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EquipmentMonitoring.Application.DTOs.Common;
using EquipmentMonitoring.Application.DTOs.Warning;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Tests.Infrastructure.Postgres;

namespace EquipmentMonitoring.Tests.API.Postgres
{
    [Collection(PostgresCollection.Name)]
    [Trait("Category", "PostgreSQL")]
    public class PostgresHttpTests(PostgresServer postgres)
    {
        private static readonly DateTime MeasuredAtUtc = MonitoringApiFactory.StartUtc.AddMinutes(-30);

        [Fact]
        public async Task MonitoringScenario_ShouldWorkAgainstPostgres()
        {
            await using var factory = await PostgresApiFactory.CreateAsync(postgres);
            using var client = factory.CreateClient();

            var equipmentId = await client.CreateEquipmentAsync("SN-PG-HTTP-1", "Pump P-7");
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/equipment", new
            {
                name = "Pump P-8",
                type = "Pump",
                serialNumber = "SN-PG-HTTP-1",
                location = "Workshop 1",
                status = "Working"
            })).StatusCode);

            Assert.True((await client.PutGlobalThresholdAsync(MeasurementType.Temperature, null, 80)).IsSuccessStatusCode);
            Assert.True((await client.PutEquipmentThresholdAsync(equipmentId, MeasurementType.Temperature, null, 90)).IsSuccessStatusCode);

            await client.PostMeasurementAsync(equipmentId, MeasurementType.Temperature, 85, MeasuredAtUtc);
            await client.PostMeasurementAsync(equipmentId, MeasurementType.Temperature, 95, MeasuredAtUtc.AddMinutes(1));

            var warnings = await (await client.GetAsync($"/api/warnings?equipmentId={equipmentId}")).ReadAsAsync<PagedResultDto<WarningDto>>();
            var warning = Assert.Single(warnings.Items);
            Assert.Equal((95d, 90d, ThresholdScope.Equipment), (warning.ActualValue, warning.LimitValue, warning.ThresholdScope));

            var summary = await (await client.GetAsync("/api/warnings/summary")).ReadAsAsync<WarningSummaryDto>();
            Assert.Equal(1, summary.TotalActive);

            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/warnings/{warning.Id}/acknowledge", null)).StatusCode);
            Assert.Equal(0, (await (await client.GetAsync("/api/warnings/summary")).ReadAsAsync<WarningSummaryDto>()).TotalActive);

            using var history = JsonDocument.Parse(await client.GetStringAsync($"/api/equipment/{equipmentId}/measurements"));
            Assert.Equal(
                [95d, 85d],
                history.RootElement.EnumerateArray().Select(m => m.GetProperty("value").GetDouble()));
        }

        [Fact]
        public async Task MaintenancePlans_ShouldBeStoredAndListedByDate()
        {
            await using var factory = await PostgresApiFactory.CreateAsync(postgres);
            using var client = factory.CreateClient();
            var equipmentId = await client.CreateEquipmentAsync("SN-PG-HTTP-2");
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            foreach (var days in new[] { 30, 7, 14 })
            {
                var response = await client.PostAsJsonAsync("/api/maintenance-plans", new
                {
                    equipmentId,
                    plannedDate = today.AddDays(days).ToString("yyyy-MM-dd"),
                    workType = $"Work in {days} days",
                    responsibleEmployee = "Employee A",
                    note = (string?)null
                });
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            }

            using var plans = JsonDocument.Parse(await client.GetStringAsync($"/api/maintenance-plans?equipmentId={equipmentId}"));
            Assert.Equal(
                [today.AddDays(7), today.AddDays(14), today.AddDays(30)],
                plans.RootElement.EnumerateArray().Select(p => DateOnly.Parse(p.GetProperty("plannedDate").GetString()!)));
        }

        [Fact]
        public async Task UnknownEquipment_ShouldReturn404()
        {
            await using var factory = await PostgresApiFactory.CreateAsync(postgres);
            using var client = factory.CreateClient();
            var unknownId = Guid.NewGuid();

            var measurement = await client.PostAsJsonAsync($"/api/equipment/{unknownId}/measurements", new
            {
                type = "Load",
                value = 50,
                unit = "%",
                measuredAt = MeasuredAtUtc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")
            });
            var plan = await client.PostAsJsonAsync("/api/maintenance-plans", new
            {
                equipmentId = unknownId,
                plannedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)).ToString("yyyy-MM-dd"),
                workType = "Inspection",
                responsibleEmployee = "Employee A"
            });

            Assert.Equal(HttpStatusCode.NotFound, measurement.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, plan.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PutEquipmentThresholdAsync(unknownId, MeasurementType.Load, null, 90)).StatusCode);
        }

        [Fact]
        public async Task OpenApi_ShouldBeServedInDockerEnvironment()
        {
            await using var factory = await PostgresApiFactory.CreateAsync(postgres);
            using var client = factory.CreateClient();

            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/openapi/v1.json")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
        }
    }
}
