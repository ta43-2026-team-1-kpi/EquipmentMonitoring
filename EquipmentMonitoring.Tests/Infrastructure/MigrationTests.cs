using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Tests.Infrastructure
{
    public class MigrationTests
    {
        [Fact]
        public async Task Model_ShouldNotHaveChangesMissingFromMigrations()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            await using var context = database.CreateContext();

            Assert.False(context.Database.HasPendingModelChanges());
        }
    }
}
