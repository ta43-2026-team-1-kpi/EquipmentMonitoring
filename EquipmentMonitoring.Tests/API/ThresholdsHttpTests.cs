using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EquipmentMonitoring.Application.DTOs.Threshold;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Tests.API
{
    public class ThresholdsHttpTests
    {
        [Fact]
        public async Task PutGlobal_WhenMissing_ShouldReturn201WithThreshold()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();

            var response = await client.PutGlobalThresholdAsync(MeasurementType.Temperature, -10, 80);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("/api/thresholds/Temperature", response.Headers.Location?.OriginalString);
            var threshold = await response.ReadAsAsync<ThresholdDto>();
            Assert.Equal(ThresholdScope.Global, threshold.Scope);
            Assert.Null(threshold.EquipmentId);
            Assert.Equal("°C", threshold.Unit);
            Assert.Equal(-10, threshold.MinValue);
            Assert.Equal(80, threshold.MaxValue);
        }

        [Fact]
        public async Task PutGlobal_WhenExisting_ShouldReturn200AndKeepId()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var created = await (await client.PutGlobalThresholdAsync(MeasurementType.Load, null, 90)).ReadAsAsync<ThresholdDto>();

            var response = await client.PutGlobalThresholdAsync(MeasurementType.Load, 10, 95);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = await response.ReadAsAsync<ThresholdDto>();
            Assert.Equal(created.Id, updated.Id);
            Assert.Equal(10, updated.MinValue);
            Assert.Equal(95, updated.MaxValue);
        }

        [Theory]
        [InlineData("temperature")]
        [InlineData("TEMPERATURE")]
        public async Task PutGlobal_ShouldAcceptMeasurementTypeCaseInsensitively(string type)
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();

            var response = await client.PutAsJsonAsync($"/api/thresholds/{type}", new { maxValue = 80 });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(MeasurementType.Temperature, (await response.ReadAsAsync<ThresholdDto>()).MeasurementType);
        }

        [Theory]
        [InlineData("Pressure")]
        [InlineData("0")]
        [InlineData("Temperature,Load")]
        public async Task PutGlobal_WithUnknownMeasurementType_ShouldReturn400(string type)
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();

            var response = await client.PutAsJsonAsync($"/api/thresholds/{type}", new { maxValue = 80 });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("measurementType", (await response.ReadValidationErrorsAsync()).Keys);
        }

        [Fact]
        public async Task PutGlobal_WithMinGreaterThanMax_ShouldReturn400WithFieldErrors()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();

            var response = await client.PutGlobalThresholdAsync(MeasurementType.Load, 90, 10);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var errors = await response.ReadValidationErrorsAsync();
            Assert.Contains(nameof(UpsertThresholdDto.MinValue), errors.Keys);
            Assert.Contains(nameof(UpsertThresholdDto.MaxValue), errors.Keys);
        }

        [Fact]
        public async Task PutGlobal_WithoutLimits_ShouldReturn400()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();

            var response = await client.PutAsync("/api/thresholds/Load", new StringContent("{}", Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ErrorResponses_ShouldBeStandardProblemDetails()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();

            var badRequest = await client.PutGlobalThresholdAsync(MeasurementType.Load, 90, 10);
            var notFound = await client.GetAsync("/api/thresholds/Load");

            foreach (var (response, status) in new[] { (badRequest, 400), (notFound, 404) })
            {
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Assert.Equal(status, json.RootElement.GetProperty("status").GetInt32());
                Assert.StartsWith("https://tools.ietf.org/html/rfc9110#section-15.5.", json.RootElement.GetProperty("type").GetString());
                Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("title").GetString()));
                Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("traceId").GetString()));
            }
        }

        [Theory]
        [InlineData("{\"maxValue\": \"abc\"}")]
        [InlineData("{\"maxValue\": NaN}")]
        [InlineData("not json")]
        public async Task PutGlobal_WithMalformedBody_ShouldReturn400(string body)
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();

            var response = await client.PutAsync("/api/thresholds/Load", new StringContent(body, Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetGlobal_ShouldReturn404WhenMissingAnd200WhenPresent()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();

            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/thresholds/Voltage")).StatusCode);

            await client.PutGlobalThresholdAsync(MeasurementType.Voltage, 200, 240);
            var response = await client.GetAsync("/api/thresholds/Voltage");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(240, (await response.ReadAsAsync<ThresholdDto>()).MaxValue);
        }

        [Fact]
        public async Task ListGlobal_ShouldNotIncludeEquipmentThresholds()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var equipmentId = await client.CreateEquipmentAsync("SN-TH-001");
            await client.PutGlobalThresholdAsync(MeasurementType.Load, null, 90);
            await client.PutEquipmentThresholdAsync(equipmentId, MeasurementType.Load, null, 70);

            var thresholds = await (await client.GetAsync("/api/thresholds")).ReadAsAsync<List<ThresholdDto>>();

            var single = Assert.Single(thresholds);
            Assert.Equal(ThresholdScope.Global, single.Scope);
        }

        [Fact]
        public async Task DeleteGlobal_ShouldReturn204ThenNotFound()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            await client.PutGlobalThresholdAsync(MeasurementType.Load, null, 90);

            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/thresholds/Load")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/thresholds/Load")).StatusCode);
        }

        [Fact]
        public async Task PutEquipment_ShouldCreateThresholdScopedToEquipment()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var equipmentId = await client.CreateEquipmentAsync("SN-TH-002");

            var response = await client.PutEquipmentThresholdAsync(equipmentId, MeasurementType.Voltage, 210, 230);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal($"/api/equipment/{equipmentId}/thresholds/Voltage", response.Headers.Location?.OriginalString);
            var threshold = await response.ReadAsAsync<ThresholdDto>();
            Assert.Equal(ThresholdScope.Equipment, threshold.Scope);
            Assert.Equal(equipmentId, threshold.EquipmentId);
        }

        [Fact]
        public async Task EquipmentEndpoints_ForUnknownEquipment_ShouldReturn404()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var unknownId = Guid.NewGuid();

            Assert.Equal(HttpStatusCode.NotFound, (await client.PutEquipmentThresholdAsync(unknownId, MeasurementType.Load, null, 90)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/equipment/{unknownId}/thresholds")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/equipment/{unknownId}/thresholds/Load")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/equipment/{unknownId}/thresholds/effective")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/equipment/{unknownId}/thresholds/Load")).StatusCode);
        }

        [Fact]
        public async Task GetEffective_ShouldCombineGlobalAndEquipmentThresholds()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var equipmentId = await client.CreateEquipmentAsync("SN-TH-003");
            await client.PutGlobalThresholdAsync(MeasurementType.Temperature, -10, 80);
            await client.PutGlobalThresholdAsync(MeasurementType.Load, null, 90);
            await client.PutEquipmentThresholdAsync(equipmentId, MeasurementType.Load, null, 70);

            var effective = await (await client.GetAsync($"/api/equipment/{equipmentId}/thresholds/effective")).ReadAsAsync<List<ThresholdDto>>();

            Assert.Equal(2, effective.Count);
            Assert.Equal(ThresholdScope.Global, effective.Single(t => t.MeasurementType == MeasurementType.Temperature).Scope);
            var load = effective.Single(t => t.MeasurementType == MeasurementType.Load);
            Assert.Equal(ThresholdScope.Equipment, load.Scope);
            Assert.Equal(70, load.MaxValue);
        }

        [Fact]
        public async Task DeleteEquipment_ShouldFallBackToGlobalInEffectiveView()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();
            var equipmentId = await client.CreateEquipmentAsync("SN-TH-004");
            await client.PutGlobalThresholdAsync(MeasurementType.Load, null, 90);
            await client.PutEquipmentThresholdAsync(equipmentId, MeasurementType.Load, null, 70);

            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/equipment/{equipmentId}/thresholds/Load")).StatusCode);

            var effective = await (await client.GetAsync($"/api/equipment/{equipmentId}/thresholds/effective")).ReadAsAsync<List<ThresholdDto>>();
            Assert.Equal(ThresholdScope.Global, Assert.Single(effective).Scope);
        }
    }
}
