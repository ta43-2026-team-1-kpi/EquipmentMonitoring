using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Application.Monitoring;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Infrastructure.Persistence;
using EquipmentMonitoring.Infrastructure.Persistence.Repositories;
using EquipmentMonitoring.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EquipmentMonitoring.Tests.Infrastructure
{
    public class MeasurementWarningAtomicityTests
    {
        private static readonly DateTime NowUtc = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task MeasurementAndWarning_ShouldBeCommittedTogether()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var equipment = await database.AddEquipmentAsync("SN-AT-001");
            await SeedGlobalThresholdAsync(database);

            await using (var context = database.CreateContext())
            {
                var measurements = new MeasurementRepository(context);
                var measurement = new Measurement(equipment.Id, MeasurementType.Load, 95, "%", NowUtc);

                await measurements.AddAsync(measurement);
                await CreateMonitor(context).InspectAsync(measurement);
                await measurements.SaveChangesAsync();
            }

            await using var readContext = database.CreateContext();
            var warning = await readContext.Warnings.SingleAsync();
            Assert.Equal((await readContext.Measurements.SingleAsync()).Id, warning.MeasurementId);
        }

        [Fact]
        public async Task FailedMeasurementSave_ShouldNotLeaveOrphanWarnings()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            await SeedGlobalThresholdAsync(database);
            var removedEquipmentId = Guid.NewGuid();

            await using (var context = database.CreateContext())
            {
                var measurements = new MeasurementRepository(context);
                var measurement = new Measurement(removedEquipmentId, MeasurementType.Load, 95, "%", NowUtc);

                await measurements.AddAsync(measurement);
                var staged = await CreateMonitor(context).InspectAsync(measurement);

                Assert.Single(staged);
                await Assert.ThrowsAsync<MeasurementEquipmentNotFoundException>(() => measurements.SaveChangesAsync());
            }

            await using var readContext = database.CreateContext();
            Assert.False(await readContext.Measurements.AnyAsync());
            Assert.False(await readContext.Warnings.AnyAsync());
        }

        private static MeasurementMonitor CreateMonitor(EquipmentMonitoringDbContext context)
        {
            var rule = new ThresholdBreachRule(new ThresholdRepository(context), new FixedTimeProvider(NowUtc), NullLogger<ThresholdBreachRule>.Instance);
            return new MeasurementMonitor([rule], new WarningRepository(context), NullLogger<MeasurementMonitor>.Instance);
        }

        private static async Task SeedGlobalThresholdAsync(SqliteTestDatabase database)
        {
            await using var context = database.CreateContext();
            context.Thresholds.Add(Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc));
            await context.SaveChangesAsync();
        }
    }
}
