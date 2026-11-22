using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EquipmentMonitoring.Tests.Infrastructure.Postgres
{
    /// <summary>
    /// Repositories translate PostgreSQL constraint violations (SQLSTATE 23505/23503) into application exceptions.
    /// </summary>
    [Collection(PostgresCollection.Name)]
    [Trait("Category", "PostgreSQL")]
    public class PostgresConstraintTests(PostgresServer postgres)
    {
        private static readonly DateTime NowUtc = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task Equipment_WithDuplicateSerialNumber_ShouldThrowDuplicateSerialNumberException()
        {
            await using var database = await postgres.CreateDatabaseAsync();
            await database.AddEquipmentAsync("SN-PG-DUP");

            await using var context = database.CreateContext();
            var repository = new EquipmentRepository(context);
            await repository.AddAsync(new Equipment("Pump", "Pump", "SN-PG-DUP", "Workshop 2", EquipmentStatus.Working));

            var exception = await Assert.ThrowsAsync<DuplicateEquipmentSerialNumberException>(() => repository.SaveChangesAsync());
            Assert.Equal("SN-PG-DUP", exception.SerialNumber);
            Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsType<PostgresException>(exception.InnerException?.InnerException).SqlState);
        }

        [Fact]
        public async Task Measurement_ForUnknownEquipment_ShouldThrowMeasurementEquipmentNotFoundException()
        {
            await using var database = await postgres.CreateDatabaseAsync();
            var unknownId = Guid.NewGuid();

            await using var context = database.CreateContext();
            var repository = new MeasurementRepository(context);
            await repository.AddAsync(new Measurement(unknownId, MeasurementType.Load, 50, "%", NowUtc));

            var exception = await Assert.ThrowsAsync<MeasurementEquipmentNotFoundException>(() => repository.SaveChangesAsync());
            Assert.Equal(unknownId, exception.EquipmentId);
        }

        [Fact]
        public async Task MaintenancePlan_ForUnknownEquipment_ShouldThrowMaintenancePlanEquipmentNotFoundException()
        {
            await using var database = await postgres.CreateDatabaseAsync();

            var unknownId = Guid.NewGuid();

            await using var context = database.CreateContext();
            var repository = new MaintenancePlanRepository(context);
            await repository.AddAsync(new MaintenancePlan(unknownId, new DateOnly(2026, 12, 1), "Inspection", "Employee A", null));

            var exception = await Assert.ThrowsAsync<MaintenancePlanEquipmentNotFoundException>(() => repository.SaveChangesAsync());
            Assert.Equal(unknownId, exception.EquipmentId);
        }

        [Fact]
        public async Task Threshold_ForUnknownEquipment_ShouldThrowEquipmentNotFoundException()
        {
            await using var database = await postgres.CreateDatabaseAsync();
            var unknownId = Guid.NewGuid();

            await using var context = database.CreateContext();
            var repository = new ThresholdRepository(context);
            await repository.AddAsync(Threshold.CreateForEquipment(unknownId, MeasurementType.Load, null, 90, NowUtc));

            var exception = await Assert.ThrowsAsync<EquipmentNotFoundException>(() => repository.SaveChangesAsync());
            Assert.Equal(unknownId, exception.EquipmentId);
        }

        [Fact]
        public async Task GlobalThreshold_WhenTypeAlreadyHasOne_ShouldThrowThresholdConflictException()
        {
            await using var database = await postgres.CreateDatabaseAsync();
            await AddThresholdAsync(database, Threshold.CreateGlobal(MeasurementType.Voltage, 200, 240, NowUtc));

            await using var context = database.CreateContext();
            var repository = new ThresholdRepository(context);
            await repository.AddAsync(Threshold.CreateGlobal(MeasurementType.Voltage, 210, 230, NowUtc));

            var exception = await Assert.ThrowsAsync<ThresholdConflictException>(() => repository.SaveChangesAsync());
            Assert.Null(exception.EquipmentId);
            Assert.Equal(MeasurementType.Voltage, exception.MeasurementType);
        }

        [Fact]
        public async Task EquipmentThreshold_WhenTypeAlreadyHasOne_ShouldThrowThresholdConflictException()
        {
            await using var database = await postgres.CreateDatabaseAsync();
            var equipment = await database.AddEquipmentAsync("SN-PG-THR");
            await AddThresholdAsync(database, Threshold.CreateForEquipment(equipment.Id, MeasurementType.Load, null, 90, NowUtc));

            await using var context = database.CreateContext();
            var repository = new ThresholdRepository(context);
            await repository.AddAsync(Threshold.CreateForEquipment(equipment.Id, MeasurementType.Load, null, 80, NowUtc));

            var exception = await Assert.ThrowsAsync<ThresholdConflictException>(() => repository.SaveChangesAsync());
            Assert.Equal(equipment.Id, exception.EquipmentId);
            Assert.Equal(MeasurementType.Load, exception.MeasurementType);
        }

        [Fact]
        public async Task GlobalAndEquipmentThresholds_ForSameType_ShouldCoexist()
        {
            await using var database = await postgres.CreateDatabaseAsync();
            var first = await database.AddEquipmentAsync("SN-PG-THR-1");
            var second = await database.AddEquipmentAsync("SN-PG-THR-2");

            await AddThresholdAsync(database, Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc));
            await AddThresholdAsync(database, Threshold.CreateForEquipment(first.Id, MeasurementType.Load, null, 80, NowUtc));
            await AddThresholdAsync(database, Threshold.CreateForEquipment(second.Id, MeasurementType.Load, null, 70, NowUtc));

            await using var context = database.CreateContext();
            Assert.Equal(3, await context.Thresholds.CountAsync());
        }

        [Theory]
        [InlineData("UPDATE \"Warnings\" SET \"Status\" = 'Unknown'")]
        [InlineData("UPDATE \"Warnings\" SET \"Status\" = 'Acknowledged'")]
        [InlineData("UPDATE \"Equipment\" SET \"Status\" = 'Broken'")]
        public async Task CheckConstraints_ShouldRejectInvalidRows(string sql)
        {
            await using var database = await postgres.CreateDatabaseAsync();
            var equipment = await database.AddEquipmentAsync("SN-PG-CK");
            await database.AddWarningAsync(equipment.Id, MeasurementType.Load, NowUtc);

            await using var context = database.CreateContext();
            var exception = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync(sql));

            Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        }

        private static async Task AddThresholdAsync(PostgresTestDatabase database, Threshold threshold)
        {
            await using var context = database.CreateContext();
            context.Thresholds.Add(threshold);
            await context.SaveChangesAsync();
        }
    }
}
