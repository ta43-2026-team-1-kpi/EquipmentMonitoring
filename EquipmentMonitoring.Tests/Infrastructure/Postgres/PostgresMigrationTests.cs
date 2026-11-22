using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Tests.Infrastructure.Postgres
{
    [Collection(PostgresCollection.Name)]
    [Trait("Category", "PostgreSQL")]
    public class PostgresMigrationTests(PostgresServer postgres)
    {
        [Fact]
        public async Task Migrations_ShouldApplyToEmptyDatabase()
        {
            await using var database = await postgres.CreateEmptyDatabaseAsync();
            await using var context = database.CreateContext();

            await context.Database.MigrateAsync();

            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
            Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync());
        }

        [Fact]
        public async Task Model_ShouldNotHaveChangesMissingFromMigrations()
        {
            await using var database = await postgres.CreateDatabaseAsync();
            await using var context = database.CreateContext();

            Assert.False(context.Database.HasPendingModelChanges());
        }
    }
}
