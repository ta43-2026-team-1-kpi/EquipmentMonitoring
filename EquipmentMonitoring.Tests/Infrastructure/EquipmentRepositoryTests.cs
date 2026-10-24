using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Infrastructure.Persistence;
using EquipmentMonitoring.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Tests.Infrastructure
{
    public class EquipmentRepositoryTests
    {
        private const int SqliteConstraintUniqueExtendedErrorCode = 2067;

        [Fact]
        public async Task AddAndSaveChangesAsync_ShouldPersistEquipmentWithAllFields()
        {
            await using var connection = await CreateOpenConnectionAsync();
            await MigrateAsync(connection);

            var expected = new Equipment(
                name: "Compressor A1",
                type: "Compressor",
                serialNumber: "SN-ADD-001",
                location: "Workshop 1",
                status: EquipmentStatus.Working);

            await using (var writeContext = CreateContext(connection))
            {
                var repository = new EquipmentRepository(writeContext);

                await repository.AddAsync(expected);
                await repository.SaveChangesAsync();
            }

            await using var readContext = CreateContext(connection);
            var actual = await readContext.Equipment.SingleAsync();

            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal("Compressor A1", actual.Name);
            Assert.Equal("Compressor", actual.Type);
            Assert.Equal("SN-ADD-001", actual.SerialNumber);
            Assert.Equal("Workshop 1", actual.Location);
            Assert.Equal(EquipmentStatus.Working, actual.Status);
        }

        [Fact]
        public async Task GetAllAsync_WithoutFilter_ShouldReturnAllRecords()
        {
            await using var connection = await CreateOpenConnectionAsync();
            await MigrateAsync(connection);

            await using (var writeContext = CreateContext(connection))
            {
                var repository = new EquipmentRepository(writeContext);

                await repository.AddAsync(new Equipment("Pump A", "Pump", "SN-ALL-001", "Zone A", EquipmentStatus.Working));
                await repository.AddAsync(new Equipment("Motor B", "Motor", "SN-ALL-002", "Zone B", EquipmentStatus.Faulty));
                await repository.SaveChangesAsync();
            }

            await using var readContext = CreateContext(connection);
            var readRepository = new EquipmentRepository(readContext);

            var result = await readRepository.GetAllAsync();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.SerialNumber == "SN-ALL-001");
            Assert.Contains(result, x => x.SerialNumber == "SN-ALL-002");
        }

        [Theory]
        [InlineData(EquipmentStatus.Working)]
        [InlineData(EquipmentStatus.Faulty)]
        [InlineData(EquipmentStatus.UnderMaintenance)]
        public async Task GetAllAsync_WithStatus_ShouldReturnOnlyMatchingStatus(EquipmentStatus requestedStatus)
        {
            await using var connection = await CreateOpenConnectionAsync();
            await MigrateAsync(connection);

            await using (var writeContext = CreateContext(connection))
            {
                var repository = new EquipmentRepository(writeContext);

                await repository.AddAsync(new Equipment("Equipment W", "Type", "SN-ST-001", "L1", EquipmentStatus.Working));
                await repository.AddAsync(new Equipment("Equipment F", "Type", "SN-ST-002", "L2", EquipmentStatus.Faulty));
                await repository.AddAsync(new Equipment("Equipment M", "Type", "SN-ST-003", "L3", EquipmentStatus.UnderMaintenance));
                await repository.SaveChangesAsync();
            }

            await using var readContext = CreateContext(connection);
            var readRepository = new EquipmentRepository(readContext);

            var result = await readRepository.GetAllAsync(requestedStatus);

            Assert.Single(result);
            Assert.All(result, x => Assert.Equal(requestedStatus, x.Status));
        }

        [Fact]
        public async Task SaveChangesAsync_WithDuplicateSerialNumber_ShouldThrowDbUpdateExceptionWithSqliteUniqueCode()
        {
            await using var connection = await CreateOpenConnectionAsync();
            await MigrateAsync(connection);

            await using var context = CreateContext(connection);
            var repository = new EquipmentRepository(context);

            await repository.AddAsync(new Equipment("Equipment A", "Type", "SN-UNIQ-001", "L1", EquipmentStatus.Working));
            await repository.SaveChangesAsync();

            await repository.AddAsync(new Equipment("Equipment B", "Type", "SN-UNIQ-001", "L2", EquipmentStatus.Faulty));

            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => repository.SaveChangesAsync());

            var sqliteException = exception.InnerException as SqliteException;
            Assert.NotNull(sqliteException);
            Assert.Equal(SqliteConstraintUniqueExtendedErrorCode, sqliteException.SqliteExtendedErrorCode);
        }

        [Fact]
        public async Task GetAllAsync_WithStatusAndNoMatches_ShouldReturnEmptyList()
        {
            await using var connection = await CreateOpenConnectionAsync();
            await MigrateAsync(connection);

            await using (var writeContext = CreateContext(connection))
            {
                var repository = new EquipmentRepository(writeContext);

                await repository.AddAsync(new Equipment("Equipment W", "Type", "SN-NONE-001", "L1", EquipmentStatus.Working));
                await repository.AddAsync(new Equipment("Equipment M", "Type", "SN-NONE-002", "L2", EquipmentStatus.UnderMaintenance));
                await repository.SaveChangesAsync();
            }

            await using var readContext = CreateContext(connection);
            var readRepository = new EquipmentRepository(readContext);

            var result = await readRepository.GetAllAsync(EquipmentStatus.Faulty);

            Assert.Empty(result);
        }

        private static async Task<SqliteConnection> CreateOpenConnectionAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            return connection;
        }

        private static EquipmentMonitoringDbContext CreateContext(SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<EquipmentMonitoringDbContext>()
                .UseSqlite(connection)
                .Options;

            return new EquipmentMonitoringDbContext(options);
        }

        private static async Task MigrateAsync(SqliteConnection connection)
        {
            await using var context = CreateContext(connection);
            await context.Database.MigrateAsync();
        }
    }
}
