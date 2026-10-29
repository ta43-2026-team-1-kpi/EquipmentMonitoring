using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Tests.Infrastructure
{
    public class WarningRepositoryTests
    {
        private static readonly DateTime NowUtc = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task AddAndSaveChangesAsync_ShouldPersistWarningSnapshot()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var equipment = await database.AddEquipmentAsync("SN-WR-001");
            var measurement = await database.AddMeasurementAsync(equipment.Id, MeasurementType.Temperature, 95, NowUtc.AddMinutes(-5));
            var threshold = Threshold.CreateGlobal(MeasurementType.Temperature, -10, 80, NowUtc);
            var warning = Warning.FromBreach(measurement, threshold, threshold.Evaluate(measurement.Value)!, NowUtc);

            await using (var context = database.CreateContext())
            {
                var repository = new WarningRepository(context);
                await repository.AddAsync(warning);
                await repository.SaveChangesAsync();
            }

            await using var readContext = database.CreateContext();
            var stored = await readContext.Warnings.SingleAsync();

            Assert.Equal(equipment.Id, stored.EquipmentId);
            Assert.Equal(measurement.Id, stored.MeasurementId);
            Assert.Equal(threshold.Id, stored.ThresholdId);
            Assert.Equal(ThresholdScope.Global, stored.ThresholdScope);
            Assert.Equal(MeasurementType.Temperature, stored.MeasurementType);
            Assert.Equal(95, stored.ActualValue);
            Assert.Equal("°C", stored.Unit);
            Assert.Equal(ThresholdBreachType.AboveMaximum, stored.BreachType);
            Assert.Equal(80, stored.LimitValue);
            Assert.Equal(WarningStatus.Active, stored.Status);
            Assert.Null(stored.AcknowledgedAtUtc);
            Assert.Equal(DateTimeKind.Utc, stored.MeasuredAtUtc.Kind);
            Assert.Equal(DateTimeKind.Utc, stored.CreatedAtUtc.Kind);
        }

        [Fact]
        public async Task Warning_ShouldNotRequireThresholdToExist()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var equipment = await database.AddEquipmentAsync("SN-WR-002");
            var measurement = await database.AddMeasurementAsync(equipment.Id, MeasurementType.Load, 99, NowUtc);
            var neverPersistedThreshold = Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc);

            await using var context = database.CreateContext();
            var repository = new WarningRepository(context);
            await repository.AddAsync(Warning.FromBreach(measurement, neverPersistedThreshold, neverPersistedThreshold.Evaluate(99)!, NowUtc));

            await repository.SaveChangesAsync();

            Assert.Equal(1, await context.Warnings.CountAsync());
        }

        [Fact]
        public async Task SaveChangesAsync_WithDuplicateWarningForSameMeasurementAndThreshold_ShouldFail()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var equipment = await database.AddEquipmentAsync("SN-WR-003");
            var measurement = await database.AddMeasurementAsync(equipment.Id, MeasurementType.Load, 99, NowUtc);
            var threshold = Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc);

            await using var context = database.CreateContext();
            var repository = new WarningRepository(context);
            await repository.AddAsync(Warning.FromBreach(measurement, threshold, threshold.Evaluate(99)!, NowUtc));
            await repository.AddAsync(Warning.FromBreach(measurement, threshold, threshold.Evaluate(99)!, NowUtc));

            await Assert.ThrowsAsync<DbUpdateException>(() => repository.SaveChangesAsync());
        }

        [Fact]
        public async Task FindAsync_ShouldReturnTrackedWarningWithEquipment()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (equipment, warnings) = await SeedAsync(database, count: 1);

            await using (var context = database.CreateContext())
            {
                var repository = new WarningRepository(context);
                var warning = await repository.FindAsync(warnings[0].Id);

                Assert.NotNull(warning);
                Assert.Equal(equipment.Name, warning.Equipment.Name);

                warning.Acknowledge(NowUtc.AddHours(1));
                await repository.SaveChangesAsync();
            }

            await using var readContext = database.CreateContext();
            var stored = await readContext.Warnings.SingleAsync();
            Assert.Equal(WarningStatus.Acknowledged, stored.Status);
            Assert.Equal(NowUtc.AddHours(1), stored.AcknowledgedAtUtc);
            Assert.Equal(DateTimeKind.Utc, stored.AcknowledgedAtUtc!.Value.Kind);
        }

        [Fact]
        public async Task FindAsync_WithUnknownId_ShouldReturnNull()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            await using var context = database.CreateContext();

            Assert.Null(await new WarningRepository(context).FindAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task SearchAsync_ShouldFilterByEquipmentStatusAndType()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (equipment, warnings) = await SeedAsync(database, count: 3);
            var (otherEquipment, _) = await SeedAsync(database, count: 2, serialNumber: "SN-WR-OTHER");

            await using (var context = database.CreateContext())
            {
                var repository = new WarningRepository(context);
                var toAcknowledge = await repository.FindAsync(warnings[0].Id);
                toAcknowledge!.Acknowledge(NowUtc.AddHours(1));
                await repository.SaveChangesAsync();
            }

            await using var readContext = database.CreateContext();
            var readRepository = new WarningRepository(readContext);

            var activeForEquipment = await readRepository.SearchAsync(new WarningSearchCriteria(equipment.Id, WarningStatus.Active, null, 1, 50));
            var acknowledged = await readRepository.SearchAsync(new WarningSearchCriteria(null, WarningStatus.Acknowledged, null, 1, 50));
            var allStatuses = await readRepository.SearchAsync(new WarningSearchCriteria(null, null, null, 1, 50));
            var voltageOnly = await readRepository.SearchAsync(new WarningSearchCriteria(null, null, MeasurementType.Voltage, 1, 50));

            Assert.Equal(2, activeForEquipment.TotalCount);
            Assert.All(activeForEquipment.Items, w => Assert.Equal(equipment.Id, w.EquipmentId));
            Assert.Single(acknowledged.Items);
            Assert.Equal(5, allStatuses.TotalCount);
            Assert.Empty(voltageOnly.Items);
            Assert.All(allStatuses.Items, w => Assert.NotNull(w.Equipment));
            Assert.Contains(allStatuses.Items, w => w.EquipmentId == otherEquipment.Id);
        }

        [Fact]
        public async Task SearchAsync_ShouldSortNewestFirstAndPaginate()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (_, warnings) = await SeedAsync(database, count: 5);

            await using var context = database.CreateContext();
            var repository = new WarningRepository(context);

            var firstPage = await repository.SearchAsync(new WarningSearchCriteria(null, null, null, 1, 2));
            var lastPage = await repository.SearchAsync(new WarningSearchCriteria(null, null, null, 3, 2));
            var beyondLastPage = await repository.SearchAsync(new WarningSearchCriteria(null, null, null, 4, 2));

            Assert.Equal(5, firstPage.TotalCount);
            Assert.Equal([warnings[4].Id, warnings[3].Id], firstPage.Items.Select(w => w.Id));
            Assert.Equal([warnings[0].Id], lastPage.Items.Select(w => w.Id));
            Assert.Empty(beyondLastPage.Items);
            Assert.Equal(5, beyondLastPage.TotalCount);
        }

        [Fact]
        public async Task DeletingEquipment_ShouldCascadeToWarnings()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (equipment, _) = await SeedAsync(database, count: 2);

            await using (var context = database.CreateContext())
            {
                await context.Equipment.Where(e => e.Id == equipment.Id).ExecuteDeleteAsync();
            }

            await using var readContext = database.CreateContext();
            Assert.False(await readContext.Warnings.AnyAsync());
            Assert.False(await readContext.Measurements.AnyAsync());
        }

        [Fact]
        public async Task Database_ShouldRejectInconsistentAcknowledgementWrittenBypassingDomain()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            await SeedAsync(database, count: 1);
            await using var context = database.CreateContext();

            var exception = await Assert.ThrowsAsync<SqliteException>(() => context.Database.ExecuteSqlRawAsync(
                "UPDATE Warnings SET Status = 'Acknowledged', AcknowledgedAtUtc = NULL"));

            Assert.Contains("CHECK constraint failed", exception.Message);
        }

        [Fact]
        public async Task CountActiveByEquipmentAndTypeAsync_ShouldGroupByEquipmentAndMeasurementType()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var pump = await database.AddEquipmentAsync("SN-WR-PUMP", "Pump P-7");
            var motor = await database.AddEquipmentAsync("SN-WR-MOTOR", "Motor M-2");
            await database.AddWarningAsync(pump.Id, MeasurementType.Temperature, NowUtc);
            await database.AddWarningAsync(pump.Id, MeasurementType.Temperature, NowUtc.AddMinutes(10));
            await database.AddWarningAsync(pump.Id, MeasurementType.Load, NowUtc.AddMinutes(5));
            await database.AddWarningAsync(motor.Id, MeasurementType.Voltage, NowUtc.AddMinutes(1));

            await using var context = database.CreateContext();
            var counts = await new WarningRepository(context).CountActiveByEquipmentAndTypeAsync();

            Assert.Equal(3, counts.Count);
            var pumpTemperature = Assert.Single(counts, c => c.EquipmentId == pump.Id && c.MeasurementType == MeasurementType.Temperature);
            Assert.Equal(2, pumpTemperature.Count);
            Assert.Equal(NowUtc.AddMinutes(10), pumpTemperature.LatestCreatedAtUtc);
            Assert.Equal("Pump P-7", pumpTemperature.EquipmentName);
            Assert.Equal("SN-WR-PUMP", pumpTemperature.EquipmentSerialNumber);
            Assert.Equal(1, Assert.Single(counts, c => c.EquipmentId == pump.Id && c.MeasurementType == MeasurementType.Load).Count);
            Assert.Equal(1, Assert.Single(counts, c => c.EquipmentId == motor.Id).Count);
        }

        [Fact]
        public async Task CountActiveByEquipmentAndTypeAsync_ShouldIgnoreAcknowledgedWarnings()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var equipment = await database.AddEquipmentAsync("SN-WR-ACK");
            await database.AddWarningAsync(equipment.Id, MeasurementType.Load, NowUtc);
            await database.AddWarningAsync(equipment.Id, MeasurementType.Load, NowUtc.AddMinutes(10), acknowledged: true);
            var onlyAcknowledged = await database.AddEquipmentAsync("SN-WR-ACK-2");
            await database.AddWarningAsync(onlyAcknowledged.Id, MeasurementType.Voltage, NowUtc, acknowledged: true);

            await using var context = database.CreateContext();
            var counts = await new WarningRepository(context).CountActiveByEquipmentAndTypeAsync();

            var count = Assert.Single(counts);
            Assert.Equal(equipment.Id, count.EquipmentId);
            Assert.Equal(1, count.Count);
            Assert.Equal(NowUtc, count.LatestCreatedAtUtc);
        }

        private static async Task<(Equipment Equipment, List<Warning> Warnings)> SeedAsync(
            SqliteTestDatabase database,
            int count,
            string serialNumber = "SN-WR-SEED")
        {
            var equipment = await database.AddEquipmentAsync(serialNumber);
            var threshold = Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc);
            var warnings = new List<Warning>();

            for (var i = 0; i < count; i++)
            {
                var measurement = await database.AddMeasurementAsync(equipment.Id, MeasurementType.Load, 91 + i, NowUtc.AddMinutes(i));
                warnings.Add(Warning.FromBreach(measurement, threshold, threshold.Evaluate(measurement.Value)!, NowUtc.AddMinutes(i)));
            }

            await using var context = database.CreateContext();
            var repository = new WarningRepository(context);

            foreach (var warning in warnings)
            {
                await repository.AddAsync(warning);
            }

            await repository.SaveChangesAsync();

            return (equipment, warnings);
        }
    }
}
