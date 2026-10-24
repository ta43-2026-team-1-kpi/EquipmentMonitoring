using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Infrastructure.Persistence;
using EquipmentMonitoring.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Tests.Infrastructure
{
    public class MeasurementRepositoryTests
    {
        [Fact]
        public async Task AddAndSaveChangesAsync_ShouldPersistMeasurementWithEquipmentLink()
        {
            await using var connection = await CreateOpenConnectionAsync();
            await MigrateAsync(connection);

            Guid equipmentId;
            await using (var setupContext = CreateContext(connection))
            {
                var equipmentRepository = new EquipmentRepository(setupContext);
                var equipment = new Equipment("Compressor", "Type", "SN-MR-001", "Workshop", EquipmentStatus.Working);
                await equipmentRepository.AddAsync(equipment);
                await equipmentRepository.SaveChangesAsync();
                equipmentId = equipment.Id;
            }

            var measuredAtUtc = new DateTime(2026, 10, 24, 10, 30, 0, DateTimeKind.Utc);

            await using (var writeContext = CreateContext(connection))
            {
                var repository = new MeasurementRepository(writeContext);
                await repository.AddAsync(new Measurement(equipmentId, MeasurementType.Temperature, 17.5, "°C", measuredAtUtc));
                await repository.SaveChangesAsync();
            }

            await using var readContext = CreateContext(connection);
            var stored = await readContext.Measurements.SingleAsync();

            Assert.Equal(equipmentId, stored.EquipmentId);
            Assert.Equal(MeasurementType.Temperature, stored.Type);
            Assert.Equal(17.5, stored.Value);
            Assert.Equal("°C", stored.Unit);
            Assert.Equal(DateTimeKind.Utc, stored.MeasuredAtUtc.Kind);
        }

        [Fact]
        public async Task GetByEquipmentAsync_ShouldFilterByTypeAndTimeAndSortDescending()
        {
            await using var connection = await CreateOpenConnectionAsync();
            await MigrateAsync(connection);

            Guid equipmentId;
            await using (var setupContext = CreateContext(connection))
            {
                var equipmentRepository = new EquipmentRepository(setupContext);
                var equipment = new Equipment("Pump", "Type", "SN-MR-002", "Zone", EquipmentStatus.Working);
                await equipmentRepository.AddAsync(equipment);
                await equipmentRepository.SaveChangesAsync();
                equipmentId = equipment.Id;
            }

            var t1 = new DateTime(2026, 10, 24, 9, 0, 0, DateTimeKind.Utc);
            var t2 = new DateTime(2026, 10, 24, 10, 0, 0, DateTimeKind.Utc);
            var t3 = new DateTime(2026, 10, 24, 11, 0, 0, DateTimeKind.Utc);

            await using (var writeContext = CreateContext(connection))
            {
                var repository = new MeasurementRepository(writeContext);
                await repository.AddAsync(new Measurement(equipmentId, MeasurementType.Voltage, 210, "V", t1));
                await repository.AddAsync(new Measurement(equipmentId, MeasurementType.Temperature, 20, "°C", t2));
                await repository.AddAsync(new Measurement(equipmentId, MeasurementType.Voltage, 220, "V", t3));
                await repository.SaveChangesAsync();
            }

            await using var readContext = CreateContext(connection);
            var readRepository = new MeasurementRepository(readContext);

            var result = await readRepository.GetByEquipmentAsync(
                equipmentId,
                MeasurementType.Voltage,
                t1,
                t3);

            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.Equal(MeasurementType.Voltage, item.Type));
            Assert.True(result[0].MeasuredAtUtc >= result[1].MeasuredAtUtc);
        }

        [Fact]
        public async Task AddAndSaveChangesAsync_WithUnknownEquipmentId_ShouldThrowMissingEquipmentExceptionWithSqliteFkCode()
        {
            await using var connection = await CreateOpenConnectionAsync();
            await MigrateAsync(connection);

            await using var context = CreateContext(connection);
            var repository = new MeasurementRepository(context);

            await repository.AddAsync(new Measurement(
                Guid.NewGuid(),
                MeasurementType.Load,
                75,
                "%",
                new DateTime(2026, 10, 24, 11, 0, 0, DateTimeKind.Utc)));

            var exception = await Assert.ThrowsAsync<MeasurementEquipmentNotFoundException>(() => repository.SaveChangesAsync());

            Assert.NotEqual(Guid.Empty, exception.EquipmentId);

            var dbUpdateException = Assert.IsType<DbUpdateException>(exception.InnerException);
            var sqliteException = Assert.IsType<SqliteException>(dbUpdateException.InnerException);
            Assert.Equal(787, sqliteException.SqliteExtendedErrorCode);
        }

        [Fact]
        public async Task GetByEquipmentAsync_ShouldKeepHistoriesIsolatedByEquipment()
        {
            await using var connection = await CreateOpenConnectionAsync();
            await MigrateAsync(connection);

            Guid equipmentAId;
            Guid equipmentBId;

            await using (var setupContext = CreateContext(connection))
            {
                var equipmentRepository = new EquipmentRepository(setupContext);
                var equipmentA = new Equipment("A", "Type", "SN-MR-003", "L1", EquipmentStatus.Working);
                var equipmentB = new Equipment("B", "Type", "SN-MR-004", "L2", EquipmentStatus.Working);
                await equipmentRepository.AddAsync(equipmentA);
                await equipmentRepository.AddAsync(equipmentB);
                await equipmentRepository.SaveChangesAsync();
                equipmentAId = equipmentA.Id;
                equipmentBId = equipmentB.Id;
            }

            await using (var writeContext = CreateContext(connection))
            {
                var repository = new MeasurementRepository(writeContext);
                await repository.AddAsync(new Measurement(equipmentAId, MeasurementType.Temperature, 11, "°C", new DateTime(2026, 10, 24, 10, 0, 0, DateTimeKind.Utc)));
                await repository.AddAsync(new Measurement(equipmentBId, MeasurementType.Temperature, 99, "°C", new DateTime(2026, 10, 24, 10, 0, 0, DateTimeKind.Utc)));
                await repository.SaveChangesAsync();
            }

            await using var readContext = CreateContext(connection);
            var readRepository = new MeasurementRepository(readContext);

            var result = await readRepository.GetByEquipmentAsync(equipmentAId, null, null, null);

            Assert.Single(result);
            Assert.Equal(equipmentAId, result[0].EquipmentId);
            Assert.Equal(11, result[0].Value);
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
