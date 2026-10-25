using System.Net;
using System.Text.Json;
using EquipmentMonitoring.Application.DTOs.Common;
using EquipmentMonitoring.Application.DTOs.Threshold;
using EquipmentMonitoring.Application.DTOs.Warning;
using EquipmentMonitoring.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Tests.API
{
    /// <summary>
    /// Acceptance criteria of EPC-6, verified end to end over HTTP.
    /// </summary>
    public class ThresholdMonitoringAcceptanceTests
    {
        private static readonly DateTime MeasuredAtUtc = MonitoringApiFactory.StartUtc.AddMinutes(-1);

        [Theory]
        [InlineData(MeasurementType.Temperature, -20.0, 85.0)]
        [InlineData(MeasurementType.Voltage, 207.0, 253.0)]
        [InlineData(MeasurementType.Load, 0.0, 90.0)]
        public async Task MinAndMaxCanBeSetForEachMeasurementType(MeasurementType type, double min, double max)
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var equipmentId = await client.CreateEquipmentAsync($"SN-AC1-{type}");

            Assert.Equal(HttpStatusCode.Created, (await client.PutGlobalThresholdAsync(type, min, max)).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await client.PutEquipmentThresholdAsync(equipmentId, type, min + 1, max - 1)).StatusCode);

            var global = await (await client.GetAsync($"/api/thresholds/{type}")).ReadAsAsync<ThresholdDto>();
            var specific = await (await client.GetAsync($"/api/equipment/{equipmentId}/thresholds/{type}")).ReadAsAsync<ThresholdDto>();

            Assert.Equal((min, max), (global.MinValue!.Value, global.MaxValue!.Value));
            Assert.Equal((min + 1, max - 1), (specific.MinValue!.Value, specific.MaxValue!.Value));
        }

        [Fact]
        public async Task NewMeasurementIsCheckedAutomaticallyAndBreachCreatesWarning()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var equipmentId = await client.CreateEquipmentAsync("SN-AC2");
            await client.PutGlobalThresholdAsync(MeasurementType.Voltage, 207, 253);

            await client.PostMeasurementAsync(equipmentId, MeasurementType.Voltage, 260, MeasuredAtUtc);
            await client.PostMeasurementAsync(equipmentId, MeasurementType.Voltage, 190, MeasuredAtUtc);

            var warnings = (await (await client.GetAsync("/api/warnings")).ReadAsAsync<PagedResultDto<WarningDto>>()).Items;

            Assert.Equal(2, warnings.Count);
            Assert.Contains(warnings, w => w.BreachType == ThresholdBreachType.AboveMaximum && w.LimitValue == 253 && w.ActualValue == 260);
            Assert.Contains(warnings, w => w.BreachType == ThresholdBreachType.BelowMinimum && w.LimitValue == 207 && w.ActualValue == 190);
        }

        [Fact]
        public async Task WarningContainsEquipmentTypeActualValueLimitAndTimestamps()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var equipmentId = await client.CreateEquipmentAsync("SN-AC4", "Transformer T-2");
            await client.PutGlobalThresholdAsync(MeasurementType.Temperature, null, 85);
            await client.PostMeasurementAsync(equipmentId, MeasurementType.Temperature, 97.3, MeasuredAtUtc);

            var response = await client.GetAsync("/api/warnings");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var warning = json.RootElement.GetProperty("items")[0];

            Assert.Equal(equipmentId, warning.GetProperty("equipmentId").GetGuid());
            Assert.Equal("Transformer T-2", warning.GetProperty("equipmentName").GetString());
            Assert.Equal("Temperature", warning.GetProperty("measurementType").GetString());
            Assert.Equal(97.3, warning.GetProperty("actualValue").GetDouble());
            Assert.Equal("°C", warning.GetProperty("unit").GetString());
            Assert.Equal(85, warning.GetProperty("limitValue").GetDouble());
            Assert.Equal("AboveMaximum", warning.GetProperty("breachType").GetString());
            Assert.Equal(MeasuredAtUtc, warning.GetProperty("measuredAtUtc").GetDateTimeOffset().UtcDateTime);
            Assert.Equal(MonitoringApiFactory.StartUtc, warning.GetProperty("createdAtUtc").GetDateTimeOffset().UtcDateTime);
        }

        [Theory]
        [InlineData(207.0)]
        [InlineData(230.0)]
        [InlineData(253.0)]
        public async Task ValueWithinLimitsDoesNotCreateWarning(double value)
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var equipmentId = await client.CreateEquipmentAsync("SN-AC5");
            await client.PutGlobalThresholdAsync(MeasurementType.Voltage, 207, 253);

            await client.PostMeasurementAsync(equipmentId, MeasurementType.Voltage, value, MeasuredAtUtc);

            Assert.Equal(0, await factory.QueryDbAsync(db => db.Warnings.CountAsync()));
            Assert.Equal(1, await factory.QueryDbAsync(db => db.Measurements.CountAsync()));
        }

        [Fact]
        public async Task UserCanListActiveWarnings()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var equipmentId = await client.CreateEquipmentAsync("SN-AC6");
            await client.PutGlobalThresholdAsync(MeasurementType.Load, null, 90);
            await client.PostMeasurementAsync(equipmentId, MeasurementType.Load, 95, MeasuredAtUtc);
            await client.PostMeasurementAsync(equipmentId, MeasurementType.Load, 97, MeasuredAtUtc);
            var all = (await (await client.GetAsync("/api/warnings")).ReadAsAsync<PagedResultDto<WarningDto>>()).Items;
            await client.PostAsync($"/api/warnings/{all[0].Id}/acknowledge", null);

            var active = await (await client.GetAsync("/api/warnings")).ReadAsAsync<PagedResultDto<WarningDto>>();

            var remaining = Assert.Single(active.Items);
            Assert.Equal(all[1].Id, remaining.Id);
            Assert.Equal(WarningStatus.Active, remaining.Status);
        }

        [Fact]
        public async Task WithoutThresholdsMeasurementsAreRecordedAsBefore()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var equipmentId = await client.CreateEquipmentAsync("SN-AC7");

            var response = await client.PostAsync(
                $"/api/equipment/{equipmentId}/measurements",
                new StringContent("""{"type":"Load","value":1000,"unit":"%","measuredAt":"2026-10-25T11:59:00Z"}""", System.Text.Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(
                ["id", "equipmentId", "type", "value", "unit", "measuredAtUtc"],
                json.RootElement.EnumerateObject().Select(p => p.Name));
            Assert.Equal(0, await factory.QueryDbAsync(db => db.Warnings.CountAsync()));
        }

        [Fact]
        public async Task ChangingOrDeletingThresholdDoesNotRewriteExistingWarnings()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var equipmentId = await client.CreateEquipmentAsync("SN-AC8");
            await client.PutGlobalThresholdAsync(MeasurementType.Load, null, 90);
            await client.PostMeasurementAsync(equipmentId, MeasurementType.Load, 95, MeasuredAtUtc);

            await client.PutGlobalThresholdAsync(MeasurementType.Load, null, 99);
            await client.DeleteAsync("/api/thresholds/Load");

            var warning = Assert.Single((await (await client.GetAsync("/api/warnings")).ReadAsAsync<PagedResultDto<WarningDto>>()).Items);
            Assert.Equal(90, warning.LimitValue);
        }

        [Fact]
        public async Task NewestWarningsComeFirstEvenWhenMeasurementTimestampIsInTheFuture()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var equipmentId = await client.CreateEquipmentAsync("SN-AC9");
            await client.PutGlobalThresholdAsync(MeasurementType.Load, null, 90);

            await client.PostMeasurementAsync(equipmentId, MeasurementType.Load, 95, new DateTime(2999, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            factory.Time.Advance(TimeSpan.FromMinutes(1));
            await client.PostMeasurementAsync(equipmentId, MeasurementType.Load, 96, MeasuredAtUtc);

            var warnings = (await (await client.GetAsync("/api/warnings")).ReadAsAsync<PagedResultDto<WarningDto>>()).Items;

            Assert.Equal([96.0, 95.0], warnings.Select(w => w.ActualValue));
        }

        [Fact]
        public async Task WarningsAreIsolatedPerEquipment()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var hot = await client.CreateEquipmentAsync("SN-AC10-A");
            var cold = await client.CreateEquipmentAsync("SN-AC10-B");
            await client.PutGlobalThresholdAsync(MeasurementType.Temperature, 0, 80);

            await client.PostMeasurementAsync(hot, MeasurementType.Temperature, 90, MeasuredAtUtc);
            await client.PostMeasurementAsync(cold, MeasurementType.Temperature, 40, MeasuredAtUtc);

            var forCold = await (await client.GetAsync($"/api/warnings?equipmentId={cold}")).ReadAsAsync<PagedResultDto<WarningDto>>();
            var forHot = await (await client.GetAsync($"/api/warnings?equipmentId={hot}")).ReadAsAsync<PagedResultDto<WarningDto>>();

            Assert.Empty(forCold.Items);
            Assert.Single(forHot.Items);
        }
    }
}
