using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Application.Services;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Infrastructure.Persistence;
using EquipmentMonitoring.Infrastructure.Persistence.Repositories;
using EquipmentMonitoring.Tests.Infrastructure;
using EquipmentMonitoring.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Tests.Application
{
    public class WarningServiceTests
    {
        private static readonly DateTime NowUtc = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task AcknowledgeAsync_ShouldMarkWarningAndPersist()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var warning = await SeedWarningAsync(database);

            await using (var context = database.CreateContext())
            {
                var dto = await CreateService(context, NowUtc.AddMinutes(30)).AcknowledgeAsync(warning.Id);

                Assert.NotNull(dto);
                Assert.Equal(WarningStatus.Acknowledged, dto.Status);
                Assert.Equal(NowUtc.AddMinutes(30), dto.AcknowledgedAtUtc!.Value.UtcDateTime);
            }

            await using var readContext = database.CreateContext();
            Assert.Equal(WarningStatus.Acknowledged, (await readContext.Warnings.SingleAsync()).Status);
        }

        [Fact]
        public async Task AcknowledgeAsync_Twice_ShouldKeepFirstAcknowledgementTime()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var warning = await SeedWarningAsync(database);

            await using (var context = database.CreateContext())
            {
                await CreateService(context, NowUtc.AddMinutes(30)).AcknowledgeAsync(warning.Id);
            }

            await using var secondContext = database.CreateContext();
            var dto = await CreateService(secondContext, NowUtc.AddMinutes(45)).AcknowledgeAsync(warning.Id);

            Assert.Equal(NowUtc.AddMinutes(30), dto!.AcknowledgedAtUtc!.Value.UtcDateTime);
        }

        [Fact]
        public async Task AcknowledgeAsync_WhenAnotherRequestAcknowledgedConcurrently_ShouldReturnStoredState()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var warning = await SeedWarningAsync(database);

            await using var slowContext = database.CreateContext();
            var slowRepository = new WarningRepository(slowContext);
            var staleCopy = await slowRepository.FindAsync(warning.Id);

            await using (var fastContext = database.CreateContext())
            {
                await CreateService(fastContext, NowUtc.AddMinutes(10)).AcknowledgeAsync(warning.Id);
            }

            Assert.Equal(WarningStatus.Active, staleCopy!.Status);

            var dto = await CreateService(slowContext, NowUtc.AddMinutes(20)).AcknowledgeAsync(warning.Id);

            Assert.Equal(WarningStatus.Acknowledged, dto!.Status);
            Assert.Equal(NowUtc.AddMinutes(10), dto.AcknowledgedAtUtc!.Value.UtcDateTime);

            await using var readContext = database.CreateContext();
            Assert.Equal(NowUtc.AddMinutes(10), (await readContext.Warnings.SingleAsync()).AcknowledgedAtUtc);
        }

        [Fact]
        public async Task AcknowledgeAsync_WithUnknownId_ShouldReturnNull()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            await using var context = database.CreateContext();

            Assert.Null(await CreateService(context, NowUtc).AcknowledgeAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetAsync_ShouldReturnWarningWithEquipmentDetails()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var warning = await SeedWarningAsync(database);
            await using var context = database.CreateContext();

            var dto = await CreateService(context, NowUtc).GetAsync(warning.Id);

            Assert.NotNull(dto);
            Assert.Equal("Pump P-7", dto.EquipmentName);
            Assert.Equal("SN-WS-001", dto.EquipmentSerialNumber);
            Assert.Equal(ThresholdBreachType.AboveMaximum, dto.BreachType);
        }

        [Fact]
        public async Task SearchAsync_ShouldReturnPagedDtos()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            await SeedWarningAsync(database);
            await using var context = database.CreateContext();

            var page = await CreateService(context, NowUtc).SearchAsync(new WarningSearchCriteria(null, WarningStatus.Active, null, 1, 10));

            Assert.Equal(1, page.TotalCount);
            Assert.Equal(1, page.Page);
            Assert.Equal(10, page.PageSize);
            Assert.Equal(1, page.TotalPages);
            Assert.Single(page.Items);
        }

        [Fact]
        public async Task SearchAsync_ForUnknownEquipment_ShouldThrow()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            await using var context = database.CreateContext();

            await Assert.ThrowsAsync<EquipmentNotFoundException>(() =>
                CreateService(context, NowUtc).SearchAsync(new WarningSearchCriteria(Guid.NewGuid(), null, null, 1, 10)));
        }

        [Fact]
        public async Task GetActiveSummaryAsync_ShouldGroupByEquipmentWithMostAffectedFirst()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var motor = await database.AddEquipmentAsync("SN-WS-MOTOR", "Motor M-2");
            var pump = await database.AddEquipmentAsync("SN-WS-PUMP", "Pump P-7");
            await database.AddWarningAsync(motor.Id, MeasurementType.Voltage, NowUtc.AddMinutes(30));
            await database.AddWarningAsync(pump.Id, MeasurementType.Load, NowUtc);
            await database.AddWarningAsync(pump.Id, MeasurementType.Temperature, NowUtc.AddMinutes(5));
            await database.AddWarningAsync(pump.Id, MeasurementType.Temperature, NowUtc.AddMinutes(10));
            await database.AddWarningAsync(pump.Id, MeasurementType.Load, NowUtc.AddMinutes(15), acknowledged: true);
            await using var context = database.CreateContext();

            var summary = await CreateService(context, NowUtc).GetActiveSummaryAsync();

            Assert.Equal(4, summary.TotalActive);
            Assert.Equal([pump.Id, motor.Id], summary.Equipment.Select(e => e.EquipmentId));

            var mostAffected = summary.Equipment[0];
            Assert.Equal("Pump P-7", mostAffected.EquipmentName);
            Assert.Equal("SN-WS-PUMP", mostAffected.EquipmentSerialNumber);
            Assert.Equal(3, mostAffected.ActiveCount);
            Assert.Equal(NowUtc.AddMinutes(10), mostAffected.LatestCreatedAtUtc.UtcDateTime);
            Assert.Equal(
                [(MeasurementType.Temperature, 2), (MeasurementType.Load, 1)],
                mostAffected.ByMeasurementType.Select(t => (t.MeasurementType, t.Count)));
        }

        [Fact]
        public async Task GetActiveSummaryAsync_WithEqualCounts_ShouldPutMostRecentFirst()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var older = await database.AddEquipmentAsync("SN-WS-OLD");
            var newer = await database.AddEquipmentAsync("SN-WS-NEW");
            await database.AddWarningAsync(older.Id, MeasurementType.Load, NowUtc);
            await database.AddWarningAsync(newer.Id, MeasurementType.Load, NowUtc.AddMinutes(1));
            await using var context = database.CreateContext();

            var summary = await CreateService(context, NowUtc).GetActiveSummaryAsync();

            Assert.Equal([newer.Id, older.Id], summary.Equipment.Select(e => e.EquipmentId));
        }

        [Fact]
        public async Task GetActiveSummaryAsync_WithoutActiveWarnings_ShouldReturnEmptySummary()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var equipment = await database.AddEquipmentAsync("SN-WS-EMPTY");
            await database.AddWarningAsync(equipment.Id, MeasurementType.Load, NowUtc, acknowledged: true);
            await using var context = database.CreateContext();

            var summary = await CreateService(context, NowUtc).GetActiveSummaryAsync();

            Assert.Equal(0, summary.TotalActive);
            Assert.Empty(summary.Equipment);
        }

        private static WarningService CreateService(EquipmentMonitoringDbContext context, DateTime utcNow)
        {
            return new WarningService(
                new WarningRepository(context),
                new EquipmentRepository(context),
                new FixedTimeProvider(utcNow));
        }

        private static async Task<Warning> SeedWarningAsync(SqliteTestDatabase database)
        {
            var equipment = await database.AddEquipmentAsync("SN-WS-001", "Pump P-7");
            var measurement = await database.AddMeasurementAsync(equipment.Id, MeasurementType.Load, 97, NowUtc);
            var threshold = Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc);
            var warning = Warning.FromBreach(measurement, threshold, threshold.Evaluate(97)!, NowUtc);

            await using var context = database.CreateContext();
            context.Warnings.Add(warning);
            await context.SaveChangesAsync();

            return warning;
        }
    }
}
