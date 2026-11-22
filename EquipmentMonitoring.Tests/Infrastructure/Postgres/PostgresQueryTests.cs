using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Infrastructure.Persistence.Repositories;

namespace EquipmentMonitoring.Tests.Infrastructure.Postgres
{
    /// <summary>
    /// Queries whose SQL translation differs between SQLite and PostgreSQL: timestamptz round-trips,
    /// date ordering, nullable comparisons, grouping and paging.
    /// </summary>
    [Collection(PostgresCollection.Name)]
    [Trait("Category", "PostgreSQL")]
    public class PostgresQueryTests(PostgresServer postgres)
    {
        private static readonly DateTime NowUtc = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task MeasurementHistory_ShouldFilterByRangeAndReturnUtcNewestFirst()
        {
            await using var database = await postgres.CreateDatabaseAsync();
            var equipment = await database.AddEquipmentAsync("SN-PG-HIST");
            await database.AddMeasurementAsync(equipment.Id, MeasurementType.Load, 10, NowUtc.AddHours(-3));
            await database.AddMeasurementAsync(equipment.Id, MeasurementType.Load, 20, NowUtc.AddHours(-2));
            await database.AddMeasurementAsync(equipment.Id, MeasurementType.Load, 30, NowUtc.AddHours(-1));
            await database.AddMeasurementAsync(equipment.Id, MeasurementType.Voltage, 220, NowUtc.AddHours(-1));

            await using var context = database.CreateContext();
            var history = await new MeasurementRepository(context).GetByEquipmentAsync(
                equipment.Id, MeasurementType.Load, NowUtc.AddHours(-2), NowUtc, CancellationToken.None);

            Assert.Equal([30d, 20d], history.Select(m => m.Value));
            Assert.All(history, m => Assert.Equal(DateTimeKind.Utc, m.MeasuredAtUtc.Kind));
            Assert.Equal(NowUtc.AddHours(-1), history[0].MeasuredAtUtc);
        }

        [Fact]
        public async Task MaintenancePlans_ShouldBeOrderedByPlannedDate()
        {
            await using var database = await postgres.CreateDatabaseAsync();
            var equipment = await database.AddEquipmentAsync("SN-PG-PLAN");

            await using (var context = database.CreateContext())
            {
                context.MaintenancePlans.AddRange(
                    new MaintenancePlan(equipment.Id, new DateOnly(2027, 1, 15), "Overhaul", "Employee A", null),
                    new MaintenancePlan(equipment.Id, new DateOnly(2026, 12, 1), "Inspection", "Employee B", "Before winter"),
                    new MaintenancePlan(equipment.Id, new DateOnly(2026, 12, 20), "Calibration", "Employee C", null));
                await context.SaveChangesAsync();
            }

            await using var readContext = database.CreateContext();
            var plans = await new MaintenancePlanRepository(readContext).GetAllAsync(equipment.Id);

            Assert.Equal(
                [new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 20), new DateOnly(2027, 1, 15)],
                plans.Select(p => p.PlannedDate));
            Assert.Equal("Before winter", plans[0].Note);
        }

        [Fact]
        public async Task Thresholds_ShouldDistinguishGlobalFromEquipmentSpecific()
        {
            await using var database = await postgres.CreateDatabaseAsync();
            var equipment = await database.AddEquipmentAsync("SN-PG-APPL");
            var other = await database.AddEquipmentAsync("SN-PG-OTHER");

            await using (var context = database.CreateContext())
            {
                context.Thresholds.AddRange(
                    Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc),
                    Threshold.CreateForEquipment(equipment.Id, MeasurementType.Load, null, 80, NowUtc),
                    Threshold.CreateForEquipment(other.Id, MeasurementType.Load, null, 70, NowUtc));
                await context.SaveChangesAsync();
            }

            await using var readContext = database.CreateContext();
            var repository = new ThresholdRepository(readContext);

            var global = Assert.Single(await repository.ListAsync(null));
            Assert.Null(global.EquipmentId);
            Assert.Equal(80, (await repository.FindAsync(equipment.Id, MeasurementType.Load))?.MaxValue);
            Assert.Equal(90, (await repository.FindAsync(null, MeasurementType.Load))?.MaxValue);
            Assert.Equal(
                [80d, 90d],
                (await repository.ListApplicableAsync(equipment.Id, MeasurementType.Load)).Select(t => t.MaxValue!.Value).Order());
        }

        [Fact]
        public async Task ActiveWarningCounts_ShouldBeGroupedByEquipmentAndType()
        {
            await using var database = await postgres.CreateDatabaseAsync();
            var pump = await database.AddEquipmentAsync("SN-PG-PUMP", "Pump P-7");
            var motor = await database.AddEquipmentAsync("SN-PG-MOTOR", "Motor M-2");
            await database.AddWarningAsync(pump.Id, MeasurementType.Temperature, NowUtc);
            await database.AddWarningAsync(pump.Id, MeasurementType.Temperature, NowUtc.AddMinutes(10));
            await database.AddWarningAsync(pump.Id, MeasurementType.Load, NowUtc.AddMinutes(5));
            await database.AddWarningAsync(motor.Id, MeasurementType.Voltage, NowUtc, acknowledged: true);

            await using var context = database.CreateContext();
            var counts = await new WarningRepository(context).CountActiveByEquipmentAndTypeAsync();

            Assert.Equal(2, counts.Count);
            var temperature = Assert.Single(counts, c => c.MeasurementType == MeasurementType.Temperature);
            Assert.Equal((pump.Id, "Pump P-7", "SN-PG-PUMP", 2), (temperature.EquipmentId, temperature.EquipmentName, temperature.EquipmentSerialNumber, temperature.Count));
            Assert.Equal(NowUtc.AddMinutes(10), temperature.LatestCreatedAtUtc);
            Assert.Equal(DateTimeKind.Utc, temperature.LatestCreatedAtUtc.Kind);
        }

        [Fact]
        public async Task WarningSearch_ShouldFilterAndPageNewestFirst()
        {
            await using var database = await postgres.CreateDatabaseAsync();
            var equipment = await database.AddEquipmentAsync("SN-PG-SEARCH");
            for (var i = 0; i < 5; i++)
            {
                await database.AddWarningAsync(equipment.Id, MeasurementType.Load, NowUtc.AddMinutes(i));
            }

            await database.AddWarningAsync(equipment.Id, MeasurementType.Load, NowUtc.AddMinutes(10), acknowledged: true);

            await using var context = database.CreateContext();
            var page = await new WarningRepository(context).SearchAsync(
                new WarningSearchCriteria(equipment.Id, WarningStatus.Active, MeasurementType.Load, Page: 2, PageSize: 2));

            Assert.Equal(5, page.TotalCount);
            Assert.Equal([NowUtc.AddMinutes(2), NowUtc.AddMinutes(1)], page.Items.Select(w => w.CreatedAtUtc));
            Assert.All(page.Items, w => Assert.Equal("SN-PG-SEARCH", w.Equipment.SerialNumber));
        }

        [Fact]
        public async Task Equipment_ShouldBeFilteredByStatus()
        {
            await using var database = await postgres.CreateDatabaseAsync();
            await database.AddEquipmentAsync("SN-PG-WORKING");

            await using (var context = database.CreateContext())
            {
                context.Equipment.Add(new Equipment("Press", "Press", "SN-PG-FAULTY", "Workshop 3", EquipmentStatus.Faulty));
                await context.SaveChangesAsync();
            }

            await using var readContext = database.CreateContext();
            var repository = new EquipmentRepository(readContext);

            Assert.Equal("SN-PG-FAULTY", Assert.Single(await repository.GetAllAsync(EquipmentStatus.Faulty)).SerialNumber);
            Assert.Equal(2, (await repository.GetAllAsync()).Count);
            Assert.True(await repository.ExistsBySerialNumberAsync("SN-PG-WORKING"));
            Assert.False(await repository.ExistsBySerialNumberAsync("sn-pg-working"));
        }
    }
}
