using System.Net;
using System.Text;
using System.Text.Json;
using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EquipmentMonitoring.Tests.API
{
    public class MaintenancePlansHttpIntegrationTests
    {
        [Fact]
        public async Task Post_WithValidPlan_ShouldCreatePlannedRecord()
        {
            await using var factory = new CustomApiFactory();
            using var client = factory.CreateClient();
            var equipmentId = await CreateEquipmentAsync(client);
            var plannedDate = GetFutureDate(7);

            var response = await PostPlanAsync(
                client,
                equipmentId,
                plannedDate,
                "Annual inspection",
                "Olena Kovalenko",
                "Check the safety valves");

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var plan = json.RootElement;
            Assert.Equal(equipmentId, plan.GetProperty("equipmentId").GetGuid());
            Assert.Equal(plannedDate, plan.GetProperty("plannedDate").GetString());
            Assert.Equal("Annual inspection", plan.GetProperty("workType").GetString());
            Assert.Equal("Olena Kovalenko", plan.GetProperty("responsibleEmployee").GetString());
            Assert.Equal("Check the safety valves", plan.GetProperty("note").GetString());
            Assert.Equal("Planned", plan.GetProperty("status").GetString());
        }

        [Fact]
        public async Task Post_ForUnregisteredEquipment_ShouldReturn404()
        {
            await using var factory = new CustomApiFactory();
            using var client = factory.CreateClient();
            var plannedDate = GetFutureDate(7);

            var response = await PostPlanAsync(
                client,
                Guid.NewGuid(),
                plannedDate,
                "Annual inspection",
                "Olena Kovalenko",
                null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Get_ShouldReturnAllPlansSortedByDate()
        {
            await using var factory = new CustomApiFactory();
            using var client = factory.CreateClient();
            var equipmentId = await CreateEquipmentAsync(client);

            var dateEarly = GetFutureDate(4);
            var dateMiddle = GetFutureDate(20);
            var dateLate = GetFutureDate(30);

            await PostPlanAsync(client, equipmentId, dateLate, "Later work", "Employee A", null);
            await PostPlanAsync(client, equipmentId, dateEarly, "Earlier work", "Employee B", null);
            await PostPlanAsync(client, equipmentId, dateMiddle, "Middle work", "Employee C", null);

            var response = await client.GetAsync("/api/maintenance-plans");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(3, json.RootElement.GetArrayLength());
            Assert.Equal(dateEarly, json.RootElement[0].GetProperty("plannedDate").GetString());
            Assert.Equal(dateMiddle, json.RootElement[1].GetProperty("plannedDate").GetString());
            Assert.Equal(dateLate, json.RootElement[2].GetProperty("plannedDate").GetString());
        }

        [Fact]
        public async Task Get_WithEquipmentFilter_ShouldReturnOnlyThatEquipmentPlans()
        {
            await using var factory = new CustomApiFactory();
            using var client = factory.CreateClient();
            var requestedEquipmentId = await CreateEquipmentAsync(client);
            var otherEquipmentId = await CreateEquipmentAsync(client);

            var dateRequested = GetFutureDate(5);
            var dateOther = GetFutureDate(6);

            await PostPlanAsync(client, requestedEquipmentId, dateRequested, "Requested equipment", "Employee A", null);
            await PostPlanAsync(client, otherEquipmentId, dateOther, "Other equipment", "Employee B", null);

            var response = await client.GetAsync(
                $"/api/maintenance-plans?equipmentId={requestedEquipmentId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Single(json.RootElement.EnumerateArray());
            Assert.Equal(
                requestedEquipmentId,
                json.RootElement[0].GetProperty("equipmentId").GetGuid());
        }

        [Theory]
        [InlineData("Completed")]
        [InlineData("Cancelled")]
        public async Task PatchStatus_FromPlanned_ShouldSetRequestedTerminalStatus(string status)
        {
            await using var factory = new CustomApiFactory();
            using var client = factory.CreateClient();
            var equipmentId = await CreateEquipmentAsync(client);
            var plannedDate = GetFutureDate(7);
            var createResponse = await PostPlanAsync(
                client,
                equipmentId,
                plannedDate,
                "Annual inspection",
                "Olena Kovalenko",
                null);
            using var createdJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
            var planId = createdJson.RootElement.GetProperty("id").GetGuid();

            var response = await PatchStatusAsync(client, planId, status);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(status, json.RootElement.GetProperty("status").GetString());
        }

        [Fact]
        public async Task PatchStatus_ToPlanned_ShouldReturn400()
        {
            await using var factory = new CustomApiFactory();
            using var client = factory.CreateClient();
            var equipmentId = await CreateEquipmentAsync(client);
            var plannedDate = GetFutureDate(7);
            var createResponse = await PostPlanAsync(
                client,
                equipmentId,
                plannedDate,
                "Annual inspection",
                "Olena Kovalenko",
                null);
            using var createdJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
            var planId = createdJson.RootElement.GetProperty("id").GetGuid();

            var response = await PatchStatusAsync(client, planId, "Planned");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Post_WithPastPlannedDate_ShouldReturn400()
        {
            await using var factory = new CustomApiFactory();
            using var client = factory.CreateClient();
            var equipmentId = await CreateEquipmentAsync(client);

            var response = await PostPlanAsync(
                client,
                equipmentId,
                "2020-01-01",
                "Annual inspection",
                "Olena Kovalenko",
                null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PatchStatus_ForMissingPlan_ShouldReturn404()
        {
            await using var factory = new CustomApiFactory();
            using var client = factory.CreateClient();

            var response = await PatchStatusAsync(client, Guid.NewGuid(), "Completed");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PatchStatus_WhenPlanIsAlreadyCompleted_ShouldReturn409()
        {
            await using var factory = new CustomApiFactory();
            using var client = factory.CreateClient();
            var equipmentId = await CreateEquipmentAsync(client);
            var plannedDate = GetFutureDate(7);
            var createResponse = await PostPlanAsync(
                client,
                equipmentId,
                plannedDate,
                "Annual inspection",
                "Olena Kovalenko",
                null);
            using var createdJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
            var planId = createdJson.RootElement.GetProperty("id").GetGuid();

            var firstUpdate = await PatchStatusAsync(client, planId, "Completed");
            Assert.Equal(HttpStatusCode.OK, firstUpdate.StatusCode);

            var secondUpdate = await PatchStatusAsync(client, planId, "Cancelled");

            Assert.Equal(HttpStatusCode.Conflict, secondUpdate.StatusCode);
        }

        [Fact]
        public async Task PatchStatus_WhenPlanIsAlreadyCancelled_ShouldReturn409()
        {
            await using var factory = new CustomApiFactory();
            using var client = factory.CreateClient();
            var equipmentId = await CreateEquipmentAsync(client);
            var plannedDate = GetFutureDate(7);
            var createResponse = await PostPlanAsync(
                client,
                equipmentId,
                plannedDate,
                "Annual inspection",
                "Olena Kovalenko",
                null);
            using var createdJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
            var planId = createdJson.RootElement.GetProperty("id").GetGuid();

            var firstUpdate = await PatchStatusAsync(client, planId, "Cancelled");
            Assert.Equal(HttpStatusCode.OK, firstUpdate.StatusCode);

            var secondUpdate = await PatchStatusAsync(client, planId, "Completed");

            Assert.Equal(HttpStatusCode.Conflict, secondUpdate.StatusCode);
        }

        [Fact]
        public async Task Post_WithMissingRequiredFields_ShouldReturn400()
        {
            await using var factory = new CustomApiFactory();
            using var client = factory.CreateClient();

            const string body = """
                                {
                                  "equipmentId": "00000000-0000-0000-0000-000000000000",
                                  "plannedDate": null,
                                  "workType": " ",
                                  "responsibleEmployee": ""
                                }
                                """;
            var response = await client.PostAsync(
                "/api/maintenance-plans",
                new StringContent(body, Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private static async Task<Guid> CreateEquipmentAsync(HttpClient client)
        {
            var serialNumber = $"SN-MAINT-{Guid.NewGuid():N}";
            var body = $$"""
                         {
                           "name": "Maintenance Test Equipment",
                           "type": "Compressor",
                           "serialNumber": "{{serialNumber}}",
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

        private static Task<HttpResponseMessage> PostPlanAsync(
            HttpClient client,
            Guid equipmentId,
            string plannedDate,
            string workType,
            string responsibleEmployee,
            string? note)
        {
            var escapedWorkType = JsonSerializer.Serialize(workType);
            var escapedEmployee = JsonSerializer.Serialize(responsibleEmployee);
            var escapedNote = JsonSerializer.Serialize(note);
            var body = $$"""
                         {
                           "equipmentId": "{{equipmentId}}",
                           "plannedDate": "{{plannedDate}}",
                           "workType": {{escapedWorkType}},
                           "responsibleEmployee": {{escapedEmployee}},
                           "note": {{escapedNote}}
                         }
                         """;

            return client.PostAsync(
                "/api/maintenance-plans",
                new StringContent(body, Encoding.UTF8, "application/json"));
        }

        private static string GetFutureDate(int daysFromToday)
        {
            return DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysFromToday)).ToString("yyyy-MM-dd");
        }

        private static Task<HttpResponseMessage> PatchStatusAsync(
            HttpClient client,
            Guid planId,
            string status)
        {
            var body = $$"""{"status":"{{status}}"}""";
            return client.SendAsync(new HttpRequestMessage(
                HttpMethod.Patch,
                $"/api/maintenance-plans/{planId}/status")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }

        private sealed class CustomApiFactory : WebApplicationFactory<Program>
        {
            private readonly SqliteConnection _connection = new("Data Source=:memory:");

            protected override void ConfigureWebHost(
                Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
            {
                builder.UseSetting("ConnectionStrings:EquipmentDatabase", "Host=localhost;Port=5432;Database=test_db;Username=test_user;Password=test_password");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:EquipmentDatabase"] = "Host=localhost;Port=5432;Database=test_db;Username=test_user;Password=test_password"
                    });
                });

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
                    var context = scope.ServiceProvider
                        .GetRequiredService<EquipmentMonitoringDbContext>();
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
