using EquipmentMonitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EquipmentMonitoring.Tests.Infrastructure.Postgres
{
    /// <summary>
    /// The PostgreSQL model is configured separately from the SQLite one used by the rest of the test suite.
    /// These tests keep both models in sync, so SQLite-based tests stay representative for the PostgreSQL deployment.
    /// Provider-specific SQL (column types, index filters, check constraint bodies) is intentionally not compared.
    /// </summary>
    public class ModelParityTests
    {
        private static readonly IModel SqliteModel = CreateSqliteModel();
        private static readonly IModel PostgresModel = CreatePostgresModel();

        public static TheoryData<string> Aspects => new() { "tables", "columns", "keys", "indexes", "foreign keys", "check constraints" };

        [Theory]
        [MemberData(nameof(Aspects))]
        public void PostgresModel_ShouldMatchSqliteModel(string aspect)
        {
            var sqlite = Describe(SqliteModel, aspect);
            var postgres = Describe(PostgresModel, aspect);

            var missingInPostgres = sqlite.Except(postgres).ToList();
            var missingInSqlite = postgres.Except(sqlite).ToList();

            Assert.True(
                missingInPostgres.Count == 0 && missingInSqlite.Count == 0,
                $"Models differ in {aspect}.{Environment.NewLine}" +
                $"Only in SQLite:{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", missingInPostgres)}{Environment.NewLine}" +
                $"Only in PostgreSQL:{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", missingInSqlite)}");
        }

        [Fact]
        public void PostgresModel_ShouldDescribeAllEntities()
        {
            Assert.Equal(
                SqliteModel.GetEntityTypes().Select(e => e.ClrType).OrderBy(t => t.Name),
                PostgresModel.GetEntityTypes().Select(e => e.ClrType).OrderBy(t => t.Name));
            Assert.NotEmpty(Describe(PostgresModel, "check constraints"));
        }

        private static IReadOnlyCollection<string> Describe(IModel model, string aspect)
        {
            var entities = model.GetEntityTypes().OrderBy(e => e.ClrType.Name).ToList();

            IEnumerable<string> lines = aspect switch
            {
                "tables" => entities.Select(e => $"{e.ClrType.Name} -> {e.GetTableName()}"),
                "columns" => entities.SelectMany(e => e.GetProperties().Select(p =>
                    $"{e.ClrType.Name}.{p.Name}: {p.ClrType.Name}, nullable={p.IsNullable}, maxLength={p.GetMaxLength()}, " +
                    $"concurrencyToken={p.IsConcurrencyToken}, storedAs={p.GetValueConverter()?.ProviderClrType.Name ?? p.ClrType.Name}")),
                "keys" => entities.SelectMany(e => e.GetKeys().Select(k =>
                    $"{e.ClrType.Name} key ({Columns(k.Properties)}), primary={k.IsPrimaryKey()}")),
                "indexes" => entities.SelectMany(e => e.GetIndexes().Select(i =>
                    $"{e.ClrType.Name} index {i.GetDatabaseName()} ({Columns(i.Properties)}), unique={i.IsUnique}, filtered={i.GetFilter() is not null}")),
                "foreign keys" => entities.SelectMany(e => e.GetForeignKeys().Select(fk =>
                    $"{e.ClrType.Name} {fk.GetConstraintName()} ({Columns(fk.Properties)}) -> {fk.PrincipalEntityType.ClrType.Name}, " +
                    $"onDelete={fk.DeleteBehavior}, required={fk.IsRequired}")),
                "check constraints" => entities.SelectMany(e => e.GetCheckConstraints().Select(c =>
                    $"{e.ClrType.Name} {c.Name}")),
                _ => throw new ArgumentOutOfRangeException(nameof(aspect), aspect, null)
            };

            return lines.Order(StringComparer.Ordinal).ToList();
        }

        private static string Columns(IEnumerable<IReadOnlyProperty> properties)
        {
            return string.Join(", ", properties.Select(p => p.Name));
        }

        private static IModel CreateSqliteModel()
        {
            var options = new DbContextOptionsBuilder<EquipmentMonitoringDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

            using var context = new EquipmentMonitoringDbContext(options);
            return context.GetService<IDesignTimeModel>().Model;
        }

        private static IModel CreatePostgresModel()
        {
            var options = new DbContextOptionsBuilder<PostgresEquipmentMonitoringDbContext>()
                .UseNpgsql("Host=localhost")
                .Options;

            using var context = new PostgresEquipmentMonitoringDbContext(options);
            return context.GetService<IDesignTimeModel>().Model;
        }
    }
}
