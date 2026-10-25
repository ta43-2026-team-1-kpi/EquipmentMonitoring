using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Tests.Infrastructure
{
    public class ThresholdRepositoryTests
    {
        private static readonly DateTime NowUtc = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task AddAndSaveChangesAsync_ShouldPersistGlobalAndEquipmentThresholds()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var equipment = await database.AddEquipmentAsync("SN-TR-001");

            await using (var context = database.CreateContext())
            {
                var repository = new ThresholdRepository(context);
                await repository.AddAsync(Threshold.CreateGlobal(MeasurementType.Temperature, -10, 80, NowUtc));
                await repository.AddAsync(Threshold.CreateForEquipment(equipment.Id, MeasurementType.Temperature, null, 60, NowUtc));
                await repository.SaveChangesAsync();
            }

            await using var readContext = database.CreateContext();
            var stored = await readContext.Thresholds.OrderBy(t => t.EquipmentId).ToListAsync();

            Assert.Equal(2, stored.Count);
            Assert.Null(stored[0].EquipmentId);
            Assert.Equal(-10, stored[0].MinValue);
            Assert.Equal(equipment.Id, stored[1].EquipmentId);
            Assert.Null(stored[1].MinValue);
            Assert.Equal(60, stored[1].MaxValue);
            Assert.Equal(DateTimeKind.Utc, stored[1].CreatedAtUtc.Kind);
            Assert.Equal(DateTimeKind.Utc, stored[1].UpdatedAtUtc.Kind);
        }

        [Fact]
        public async Task SaveChangesAsync_WithSecondGlobalThresholdForSameType_ShouldThrowConflict()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();

            await using (var context = database.CreateContext())
            {
                var repository = new ThresholdRepository(context);
                await repository.AddAsync(Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc));
                await repository.SaveChangesAsync();
            }

            await using var conflictingContext = database.CreateContext();
            var conflictingRepository = new ThresholdRepository(conflictingContext);
            await conflictingRepository.AddAsync(Threshold.CreateGlobal(MeasurementType.Load, null, 95, NowUtc));

            var exception = await Assert.ThrowsAsync<ThresholdConflictException>(() => conflictingRepository.SaveChangesAsync());
            Assert.Null(exception.EquipmentId);
            Assert.Equal(MeasurementType.Load, exception.MeasurementType);
        }

        [Fact]
        public async Task SaveChangesAsync_WithSecondEquipmentThresholdForSameType_ShouldThrowConflict()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var equipment = await database.AddEquipmentAsync("SN-TR-002");

            await using (var context = database.CreateContext())
            {
                var repository = new ThresholdRepository(context);
                await repository.AddAsync(Threshold.CreateForEquipment(equipment.Id, MeasurementType.Load, null, 90, NowUtc));
                await repository.SaveChangesAsync();
            }

            await using var conflictingContext = database.CreateContext();
            var conflictingRepository = new ThresholdRepository(conflictingContext);
            await conflictingRepository.AddAsync(Threshold.CreateForEquipment(equipment.Id, MeasurementType.Load, null, 95, NowUtc));

            var exception = await Assert.ThrowsAsync<ThresholdConflictException>(() => conflictingRepository.SaveChangesAsync());
            Assert.Equal(equipment.Id, exception.EquipmentId);
        }

        [Fact]
        public async Task SaveChangesAsync_WithSameTypeOnDifferentScopes_ShouldBeAllowed()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var first = await database.AddEquipmentAsync("SN-TR-003");
            var second = await database.AddEquipmentAsync("SN-TR-004");

            await using var context = database.CreateContext();
            var repository = new ThresholdRepository(context);
            await repository.AddAsync(Threshold.CreateGlobal(MeasurementType.Voltage, 200, 240, NowUtc));
            await repository.AddAsync(Threshold.CreateForEquipment(first.Id, MeasurementType.Voltage, 210, 230, NowUtc));
            await repository.AddAsync(Threshold.CreateForEquipment(second.Id, MeasurementType.Voltage, 215, 225, NowUtc));

            await repository.SaveChangesAsync();

            Assert.Equal(3, await context.Thresholds.CountAsync());
        }

        [Fact]
        public async Task SaveChangesAsync_ForMissingEquipment_ShouldThrowEquipmentNotFound()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var missingEquipmentId = Guid.NewGuid();

            await using var context = database.CreateContext();
            var repository = new ThresholdRepository(context);
            await repository.AddAsync(Threshold.CreateForEquipment(missingEquipmentId, MeasurementType.Load, null, 90, NowUtc));

            var exception = await Assert.ThrowsAsync<EquipmentNotFoundException>(() => repository.SaveChangesAsync());
            Assert.Equal(missingEquipmentId, exception.EquipmentId);
        }

        [Fact]
        public async Task FindAsync_ShouldMatchExactScope()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var equipment = await database.AddEquipmentAsync("SN-TR-005");
            var global = Threshold.CreateGlobal(MeasurementType.Temperature, -10, 80, NowUtc);
            var specific = Threshold.CreateForEquipment(equipment.Id, MeasurementType.Temperature, 0, 60, NowUtc);

            await using (var context = database.CreateContext())
            {
                var repository = new ThresholdRepository(context);
                await repository.AddAsync(global);
                await repository.AddAsync(specific);
                await repository.SaveChangesAsync();
            }

            await using var readContext = database.CreateContext();
            var readRepository = new ThresholdRepository(readContext);

            Assert.Equal(global.Id, (await readRepository.FindAsync(null, MeasurementType.Temperature))!.Id);
            Assert.Equal(specific.Id, (await readRepository.FindAsync(equipment.Id, MeasurementType.Temperature))!.Id);
            Assert.Null(await readRepository.FindAsync(equipment.Id, MeasurementType.Voltage));
            Assert.Null(await readRepository.FindAsync(Guid.NewGuid(), MeasurementType.Temperature));
        }

        [Fact]
        public async Task ListAsync_ShouldReturnOnlyRequestedScope()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var equipment = await database.AddEquipmentAsync("SN-TR-006");
            var other = await database.AddEquipmentAsync("SN-TR-007");

            await using (var context = database.CreateContext())
            {
                var repository = new ThresholdRepository(context);
                await repository.AddAsync(Threshold.CreateGlobal(MeasurementType.Temperature, -10, 80, NowUtc));
                await repository.AddAsync(Threshold.CreateForEquipment(equipment.Id, MeasurementType.Voltage, 210, 230, NowUtc));
                await repository.AddAsync(Threshold.CreateForEquipment(equipment.Id, MeasurementType.Load, null, 90, NowUtc));
                await repository.AddAsync(Threshold.CreateForEquipment(other.Id, MeasurementType.Load, null, 50, NowUtc));
                await repository.SaveChangesAsync();
            }

            await using var readContext = database.CreateContext();
            var readRepository = new ThresholdRepository(readContext);

            var globals = await readRepository.ListAsync(null);
            var equipmentThresholds = await readRepository.ListAsync(equipment.Id);

            Assert.Single(globals);
            Assert.Equal(2, equipmentThresholds.Count);
            Assert.All(equipmentThresholds, t => Assert.Equal(equipment.Id, t.EquipmentId));
        }

        [Fact]
        public async Task ListApplicableAsync_ShouldReturnEquipmentAndGlobalThresholdsOnly()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var equipment = await database.AddEquipmentAsync("SN-TR-008");
            var other = await database.AddEquipmentAsync("SN-TR-009");

            await using (var context = database.CreateContext())
            {
                var repository = new ThresholdRepository(context);
                await repository.AddAsync(Threshold.CreateGlobal(MeasurementType.Temperature, -10, 80, NowUtc));
                await repository.AddAsync(Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc));
                await repository.AddAsync(Threshold.CreateForEquipment(equipment.Id, MeasurementType.Temperature, 0, 60, NowUtc));
                await repository.AddAsync(Threshold.CreateForEquipment(other.Id, MeasurementType.Temperature, 0, 30, NowUtc));
                await repository.SaveChangesAsync();
            }

            await using var readContext = database.CreateContext();
            var readRepository = new ThresholdRepository(readContext);

            var all = await readRepository.ListApplicableAsync(equipment.Id, null);
            var temperatureOnly = await readRepository.ListApplicableAsync(equipment.Id, MeasurementType.Temperature);

            Assert.Equal(3, all.Count);
            Assert.DoesNotContain(all, t => t.EquipmentId == other.Id);
            Assert.Equal(2, temperatureOnly.Count);
            Assert.All(temperatureOnly, t => Assert.Equal(MeasurementType.Temperature, t.MeasurementType));
        }

        [Fact]
        public async Task Remove_ShouldDeleteThreshold()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();

            await using (var context = database.CreateContext())
            {
                var repository = new ThresholdRepository(context);
                await repository.AddAsync(Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc));
                await repository.SaveChangesAsync();
            }

            await using (var context = database.CreateContext())
            {
                var repository = new ThresholdRepository(context);
                var threshold = await repository.FindAsync(null, MeasurementType.Load);
                repository.Remove(threshold!);
                await repository.SaveChangesAsync();
            }

            await using var readContext = database.CreateContext();
            Assert.False(await readContext.Thresholds.AnyAsync());
        }

        [Fact]
        public async Task DeletingEquipment_ShouldCascadeToItsThresholds()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var equipment = await database.AddEquipmentAsync("SN-TR-010");

            await using (var context = database.CreateContext())
            {
                var repository = new ThresholdRepository(context);
                await repository.AddAsync(Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc));
                await repository.AddAsync(Threshold.CreateForEquipment(equipment.Id, MeasurementType.Load, null, 80, NowUtc));
                await repository.SaveChangesAsync();
            }

            await using (var context = database.CreateContext())
            {
                await context.Equipment.Where(e => e.Id == equipment.Id).ExecuteDeleteAsync();
            }

            await using var readContext = database.CreateContext();
            var remaining = await readContext.Thresholds.SingleAsync();
            Assert.Null(remaining.EquipmentId);
        }

        [Theory]
        [InlineData("NULL", "NULL")]
        [InlineData("90", "10")]
        public async Task Database_ShouldRejectInvalidLimitsWrittenBypassingDomain(string min, string max)
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            await using var context = database.CreateContext();

            var exception = await Assert.ThrowsAsync<SqliteException>(() => context.Database.ExecuteSqlRawAsync(
                $"""
                INSERT INTO Thresholds (Id, EquipmentId, MeasurementType, MinValue, MaxValue, CreatedAtUtc, UpdatedAtUtc)
                VALUES ('{Guid.NewGuid()}', NULL, 'Load', {min}, {max}, '2026-10-25 12:00:00', '2026-10-25 12:00:00')
                """));

            Assert.Contains("CHECK constraint failed", exception.Message);
        }
    }
}
