using System.Net;
using EquipmentMonitoring.Tests.Infrastructure.Postgres;

namespace EquipmentMonitoring.Tests.API.Postgres
{
    [Collection(PostgresCollection.Name)]
    [Trait("Category", "PostgreSQL")]
    public class PostgresHealthTests(PostgresServer postgres)
    {
        [Fact]
        public async Task Readiness_ShouldFollowDatabaseAvailability_WhileLivenessStaysUp()
        {
            await using var factory = await PostgresApiFactory.CreateAsync(postgres);
            using var client = factory.CreateClient();

            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);

            await factory.DropDatabaseAsync();

            Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        }
    }
}
