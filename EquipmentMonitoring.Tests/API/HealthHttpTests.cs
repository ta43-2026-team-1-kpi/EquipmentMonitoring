using System.Net;

namespace EquipmentMonitoring.Tests.API
{
    public class HealthHttpTests
    {
        [Theory]
        [InlineData("/health/live")]
        [InlineData("/health/ready")]
        public async Task HealthEndpoint_ShouldReportHealthy(string path)
        {
            await using var factory = await MonitoringApiFactory.CreateAsync();
            using var client = factory.CreateClient();

            var response = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }
    }
}
