using System.Net;
using EquipmentMonitoring.Application.DTOs.Common;
using EquipmentMonitoring.Application.DTOs.Warning;
using EquipmentMonitoring.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Tests.API
{
    public class WarningsHttpTests
    {
        private static readonly DateTime MeasuredAtUtc = MonitoringApiFactory.StartUtc.AddMinutes(-5);

        [Fact]
        public async Task PostMeasurement_AboveThreshold_ShouldCreateActiveWarning()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var equipmentId = await client.CreateEquipmentAsync("SN-WH-001", "Pump P-7");
            await client.PutGlobalThresholdAsync(MeasurementType.Temperature, -10, 80);

            var measurementId = await client.PostMeasurementAsync(equipmentId, MeasurementType.Temperature, 92.5, MeasuredAtUtc);

            var page = await (await client.GetAsync("/api/warnings")).ReadAsAsync<PagedResultDto<WarningDto>>();
            var warning = Assert.Single(page.Items);
            Assert.Equal(equipmentId, warning.EquipmentId);
            Assert.Equal("Pump P-7", warning.EquipmentName);
            Assert.Equal("SN-WH-001", warning.EquipmentSerialNumber);
            Assert.Equal(measurementId, warning.MeasurementId);
            Assert.Equal(MeasurementType.Temperature, warning.MeasurementType);
            Assert.Equal(92.5, warning.ActualValue);
            Assert.Equal("°C", warning.Unit);
            Assert.Equal(ThresholdBreachType.AboveMaximum, warning.BreachType);
            Assert.Equal(80, warning.LimitValue);
            Assert.Equal(MeasuredAtUtc, warning.MeasuredAtUtc.UtcDateTime);
            Assert.Equal(MonitoringApiFactory.StartUtc, warning.CreatedAtUtc.UtcDateTime);
            Assert.Equal(WarningStatus.Active, warning.Status);
            Assert.Null(warning.AcknowledgedAtUtc);
        }

        [Fact]
        public async Task PostMeasurement_WithinThreshold_ShouldNotCreateWarning()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var equipmentId = await client.CreateEquipmentAsync("SN-WH-002");
            await client.PutGlobalThresholdAsync(MeasurementType.Temperature, -10, 80);

            await client.PostMeasurementAsync(equipmentId, MeasurementType.Temperature, 80, MeasuredAtUtc);

            var page = await (await client.GetAsync("/api/warnings?status=All")).ReadAsAsync<PagedResultDto<WarningDto>>();
            Assert.Empty(page.Items);
            Assert.Equal(1, await factory.QueryDbAsync(db => db.Measurements.CountAsync()));
        }

        [Fact]
        public async Task PostMeasurement_ShouldRespectEquipmentOverride()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var strict = await client.CreateEquipmentAsync("SN-WH-003");
            var regular = await client.CreateEquipmentAsync("SN-WH-004");
            await client.PutGlobalThresholdAsync(MeasurementType.Load, null, 90);
            await client.PutEquipmentThresholdAsync(strict, MeasurementType.Load, null, 70);

            await client.PostMeasurementAsync(strict, MeasurementType.Load, 75, MeasuredAtUtc);
            await client.PostMeasurementAsync(regular, MeasurementType.Load, 75, MeasuredAtUtc);

            var page = await (await client.GetAsync("/api/warnings")).ReadAsAsync<PagedResultDto<WarningDto>>();
            var warning = Assert.Single(page.Items);
            Assert.Equal(strict, warning.EquipmentId);
            Assert.Equal(ThresholdScope.Equipment, warning.ThresholdScope);
        }

        [Fact]
        public async Task GetWarnings_ShouldFilterByEquipmentStatusAndType()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var first = await client.CreateEquipmentAsync("SN-WH-005");
            var second = await client.CreateEquipmentAsync("SN-WH-006");
            await client.PutGlobalThresholdAsync(MeasurementType.Load, null, 90);
            await client.PutGlobalThresholdAsync(MeasurementType.Voltage, 200, 240);
            await client.PostMeasurementAsync(first, MeasurementType.Load, 95, MeasuredAtUtc);
            await client.PostMeasurementAsync(first, MeasurementType.Voltage, 250, MeasuredAtUtc);
            await client.PostMeasurementAsync(second, MeasurementType.Load, 99, MeasuredAtUtc);

            var all = await (await client.GetAsync("/api/warnings")).ReadAsAsync<PagedResultDto<WarningDto>>();
            var toAcknowledge = all.Items.Single(w => w.EquipmentId == second);
            await client.PostAsync($"/api/warnings/{toAcknowledge.Id}/acknowledge", null);

            var activeForFirst = await (await client.GetAsync($"/api/warnings?equipmentId={first}")).ReadAsAsync<PagedResultDto<WarningDto>>();
            var acknowledged = await (await client.GetAsync("/api/warnings?status=acknowledged")).ReadAsAsync<PagedResultDto<WarningDto>>();
            var voltage = await (await client.GetAsync("/api/warnings?status=All&measurementType=Voltage")).ReadAsAsync<PagedResultDto<WarningDto>>();
            var active = await (await client.GetAsync("/api/warnings")).ReadAsAsync<PagedResultDto<WarningDto>>();

            Assert.Equal(2, activeForFirst.TotalCount);
            Assert.All(activeForFirst.Items, w => Assert.Equal(first, w.EquipmentId));
            Assert.Equal(toAcknowledge.Id, Assert.Single(acknowledged.Items).Id);
            Assert.Equal(MeasurementType.Voltage, Assert.Single(voltage.Items).MeasurementType);
            Assert.Equal(2, active.TotalCount);
            Assert.All(active.Items, w => Assert.Equal(WarningStatus.Active, w.Status));
        }

        [Fact]
        public async Task GetWarnings_ShouldPaginateNewestFirst()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var equipmentId = await client.CreateEquipmentAsync("SN-WH-007");
            await client.PutGlobalThresholdAsync(MeasurementType.Load, null, 90);

            for (var i = 0; i < 5; i++)
            {
                await client.PostMeasurementAsync(equipmentId, MeasurementType.Load, 91 + i, MeasuredAtUtc);
                factory.Time.Advance(TimeSpan.FromMinutes(1));
            }

            var page = await (await client.GetAsync("/api/warnings?page=2&pageSize=2")).ReadAsAsync<PagedResultDto<WarningDto>>();

            Assert.Equal(5, page.TotalCount);
            Assert.Equal(3, page.TotalPages);
            Assert.Equal(2, page.Page);
            Assert.Equal([93.0, 92.0], page.Items.Select(w => w.ActualValue));
        }

        [Theory]
        [InlineData("status=Unknown", "status")]
        [InlineData("status=", "status")]
        [InlineData("status=1", "status")]
        [InlineData("measurementType=Pressure", "measurementType")]
        [InlineData("measurementType=", "measurementType")]
        [InlineData("page=0", "page")]
        [InlineData("pageSize=0", "pageSize")]
        [InlineData("pageSize=201", "pageSize")]
        public async Task GetWarnings_WithInvalidQuery_ShouldReturn400(string query, string expectedErrorKey)
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();

            var response = await client.GetAsync($"/api/warnings?{query}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(expectedErrorKey, (await response.ReadValidationErrorsAsync()).Keys);
        }

        [Theory]
        [InlineData("page=abc")]
        [InlineData("equipmentId=not-a-guid")]
        public async Task GetWarnings_WithUnparsableQuery_ShouldReturn400(string query)
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();

            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/warnings?{query}")).StatusCode);
        }

        [Fact]
        public async Task GetWarnings_ForUnknownEquipment_ShouldReturn404()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();

            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/warnings?equipmentId={Guid.NewGuid()}")).StatusCode);
        }

        [Fact]
        public async Task GetWarningById_ShouldReturnWarningOr404()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var equipmentId = await client.CreateEquipmentAsync("SN-WH-008");
            await client.PutGlobalThresholdAsync(MeasurementType.Load, null, 90);
            await client.PostMeasurementAsync(equipmentId, MeasurementType.Load, 95, MeasuredAtUtc);
            var warningId = (await (await client.GetAsync("/api/warnings")).ReadAsAsync<PagedResultDto<WarningDto>>()).Items[0].Id;

            var found = await client.GetAsync($"/api/warnings/{warningId}");

            Assert.Equal(HttpStatusCode.OK, found.StatusCode);
            Assert.Equal(warningId, (await found.ReadAsAsync<WarningDto>()).Id);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/warnings/{Guid.NewGuid()}")).StatusCode);
        }

        [Fact]
        public async Task Acknowledge_ShouldBeIdempotentAndRemoveWarningFromActiveList()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var equipmentId = await client.CreateEquipmentAsync("SN-WH-009");
            await client.PutGlobalThresholdAsync(MeasurementType.Load, null, 90);
            await client.PostMeasurementAsync(equipmentId, MeasurementType.Load, 95, MeasuredAtUtc);
            var warningId = (await (await client.GetAsync("/api/warnings")).ReadAsAsync<PagedResultDto<WarningDto>>()).Items[0].Id;
            factory.Time.Advance(TimeSpan.FromMinutes(10));

            var first = await client.PostAsync($"/api/warnings/{warningId}/acknowledge", null);
            factory.Time.Advance(TimeSpan.FromMinutes(10));
            var second = await client.PostAsync($"/api/warnings/{warningId}/acknowledge", null);

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            var acknowledged = await second.ReadAsAsync<WarningDto>();
            Assert.Equal(WarningStatus.Acknowledged, acknowledged.Status);
            Assert.Equal(MonitoringApiFactory.StartUtc.AddMinutes(10), acknowledged.AcknowledgedAtUtc!.Value.UtcDateTime);

            var active = await (await client.GetAsync("/api/warnings")).ReadAsAsync<PagedResultDto<WarningDto>>();
            Assert.Empty(active.Items);
        }

        [Fact]
        public async Task Acknowledge_UnknownWarning_ShouldReturn404()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();

            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/warnings/{Guid.NewGuid()}/acknowledge", null)).StatusCode);
        }
    }
}
