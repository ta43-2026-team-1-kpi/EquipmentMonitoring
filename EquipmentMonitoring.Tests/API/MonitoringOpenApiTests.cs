using System.Text.Json;

namespace EquipmentMonitoring.Tests.API
{
    public class MonitoringOpenApiTests
    {
        public static TheoryData<string, string> MonitoringOperations => new()
        {
            { "/api/thresholds", "get" },
            { "/api/thresholds/{measurementType}", "get" },
            { "/api/thresholds/{measurementType}", "put" },
            { "/api/thresholds/{measurementType}", "delete" },
            { "/api/equipment/{equipmentId}/thresholds", "get" },
            { "/api/equipment/{equipmentId}/thresholds/effective", "get" },
            { "/api/equipment/{equipmentId}/thresholds/{measurementType}", "get" },
            { "/api/equipment/{equipmentId}/thresholds/{measurementType}", "put" },
            { "/api/equipment/{equipmentId}/thresholds/{measurementType}", "delete" },
            { "/api/warnings", "get" },
            { "/api/warnings/summary", "get" },
            { "/api/warnings/{warningId}", "get" },
            { "/api/warnings/{warningId}/acknowledge", "post" }
        };

        [Theory]
        [MemberData(nameof(MonitoringOperations))]
        public async Task Operation_ShouldHaveSummaryDescriptionTagAndDocumentedParameters(string path, string method)
        {
            using var document = await GetDocumentAsync();
            var operation = document.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method);

            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()));
            Assert.NotEmpty(operation.GetProperty("tags").EnumerateArray());

            if (operation.TryGetProperty("parameters", out var parameters))
            {
                Assert.All(parameters.EnumerateArray(), p =>
                    Assert.False(string.IsNullOrWhiteSpace(p.GetProperty("description").GetString()), p.GetProperty("name").GetString()));
            }
        }

        [Theory]
        [InlineData("/api/thresholds/{measurementType}", "put", new[] { "200", "201", "400", "409" })]
        [InlineData("/api/equipment/{equipmentId}/thresholds/{measurementType}", "put", new[] { "200", "201", "400", "404", "409" })]
        [InlineData("/api/warnings", "get", new[] { "200", "400", "404" })]
        [InlineData("/api/warnings/summary", "get", new[] { "200" })]
        [InlineData("/api/warnings/{warningId}/acknowledge", "post", new[] { "200", "404" })]
        public async Task Operation_ShouldDocumentResponsesWithExamples(string path, string method, string[] statusCodes)
        {
            using var document = await GetDocumentAsync();
            var responses = document.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method).GetProperty("responses");

            foreach (var statusCode in statusCodes)
            {
                var response = responses.GetProperty(statusCode);
                Assert.False(string.IsNullOrWhiteSpace(response.GetProperty("description").GetString()));
                var content = Assert.Single(response.GetProperty("content").EnumerateObject());
                Assert.True(content.Value.TryGetProperty("example", out _), $"{method} {path} {statusCode} has no example");
            }
        }

        [Fact]
        public async Task ThresholdUpsert_ShouldHaveRequestExample()
        {
            using var document = await GetDocumentAsync();
            var requestBody = document.RootElement
                .GetProperty("paths").GetProperty("/api/thresholds/{measurementType}").GetProperty("put")
                .GetProperty("requestBody").GetProperty("content").GetProperty("application/json");

            Assert.True(requestBody.TryGetProperty("example", out _));
        }

        [Theory]
        [InlineData("ThresholdDto", "scope")]
        [InlineData("ThresholdDto", "minValue")]
        [InlineData("UpsertThresholdDto", "maxValue")]
        [InlineData("WarningDto", "breachType")]
        [InlineData("WarningDto", "limitValue")]
        [InlineData("WarningDto", "status")]
        [InlineData("WarningSummaryDto", "totalActive")]
        [InlineData("EquipmentWarningSummaryDto", "activeCount")]
        [InlineData("MeasurementTypeWarningCountDto", "count")]
        public async Task Schema_ShouldDescribeMonitoringProperties(string schema, string property)
        {
            using var document = await GetDocumentAsync();
            var propertySchema = document.RootElement
                .GetProperty("components").GetProperty("schemas").GetProperty(schema)
                .GetProperty("properties").GetProperty(property);

            Assert.False(string.IsNullOrWhiteSpace(propertySchema.GetProperty("description").GetString()));
        }

        private static async Task<JsonDocument> GetDocumentAsync()
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();

            return JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        }
    }
}
