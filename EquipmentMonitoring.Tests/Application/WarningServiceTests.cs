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
