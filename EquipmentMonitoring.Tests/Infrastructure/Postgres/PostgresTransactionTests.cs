using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Application.Monitoring;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Infrastructure.Persistence;
using EquipmentMonitoring.Infrastructure.Persistence.Repositories;
using EquipmentMonitoring.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EquipmentMonitoring.Tests.Infrastructure.Postgres
{
    [Collection(PostgresCollection.Name)]
    [Trait("Category", "PostgreSQL")]
    public class PostgresTransactionTests(PostgresServer postgres)
    {
        private static readonly DateTime NowUtc = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task ConcurrentAcknowledge_ShouldLetOnlyOneRequestWin()
        {
            await using var database = await postgres.CreateDatabaseAsync();
            var equipment = await database.AddEquipmentAsync("SN-PG-ACK");
            var warning = await database.AddWarningAsync(equipment.Id, MeasurementType.Load, NowUtc);

            await using var first = database.CreateContext();
            await using var second = database.CreateContext();
            var firstRepository = new WarningRepository(first);
            var secondRepository = new WarningRepository(second);
            var firstCopy = await firstRepository.FindAsync(warning.Id);
            var secondCopy = await secondRepository.FindAsync(warning.Id);

            firstCopy!.Acknowledge(NowUtc.AddMinutes(1));
            secondCopy!.Acknowledge(NowUtc.AddMinutes(2));
            await firstRepository.SaveChangesAsync();

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() => secondRepository.SaveChangesAsync());
            Assert.Equal(NowUtc.AddMinutes(1), secondCopy.AcknowledgedAtUtc);
        }

        [Fact]
        public async Task MeasurementAndWarning_ShouldBeCommittedTogether()
        {
            await using var database = await postgres.CreateDatabaseAsync();
            var equipment = await database.AddEquipmentAsync("SN-PG-ATOMIC");
            await SeedGlobalLoadThresholdAsync(database);

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
            await using var database = await postgres.CreateDatabaseAsync();
            await SeedGlobalLoadThresholdAsync(database);

            await using (var context = database.CreateContext())
            {
                var measurements = new MeasurementRepository(context);
                var measurement = new Measurement(Guid.NewGuid(), MeasurementType.Load, 95, "%", NowUtc);

                await measurements.AddAsync(measurement);
                Assert.Single(await CreateMonitor(context).InspectAsync(measurement));
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

        private static async Task SeedGlobalLoadThresholdAsync(PostgresTestDatabase database)
        {
            await using var context = database.CreateContext();
            context.Thresholds.Add(Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc));
            await context.SaveChangesAsync();
        }
    }
}
