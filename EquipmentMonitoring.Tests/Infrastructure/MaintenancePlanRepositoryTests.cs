using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Infrastructure.Persistence;
using EquipmentMonitoring.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Tests.Infrastructure
{
    public class MaintenancePlanRepositoryTests
    {
        [Fact]
        public async Task AddAndSaveChangesAsync_ShouldPersistAllMaintenancePlanFields()
        {
            await using var connection = await CreateOpenConnectionAsync();
            await MigrateAsync(connection);
            var equipment = new Equipment(
                "Compressor A1",
                "Compressor",
                "SN-MP-PERSIST-001",
                "Workshop 1",
                EquipmentStatus.Working);
            var plan = CreatePlan(equipment.Id, new DateOnly(2026, 11, 12));

            await using (var writeContext = CreateContext(connection))
            {
                await writeContext.Equipment.AddAsync(equipment);
                var repository = new MaintenancePlanRepository(writeContext);
                await repository.AddAsync(plan);
                await repository.SaveChangesAsync();
            }

            await using var readContext = CreateContext(connection);
            var savedPlan = await readContext.MaintenancePlans.SingleAsync();

            Assert.Equal(plan.Id, savedPlan.Id);
            Assert.Equal(equipment.Id, savedPlan.EquipmentId);
            Assert.Equal(new DateOnly(2026, 11, 12), savedPlan.PlannedDate);
            Assert.Equal("Annual inspection", savedPlan.WorkType);
            Assert.Equal("Olena Kovalenko", savedPlan.ResponsibleEmployee);
            Assert.Equal("Check safety valves", savedPlan.Note);
            Assert.Equal(MaintenancePlanStatus.Planned, savedPlan.Status);
        }

        [Fact]
        public async Task GetAllAsync_ShouldSortByPlannedDate()
        {
            await using var connection = await CreateOpenConnectionAsync();
            await MigrateAsync(connection);
            var equipment = new Equipment(
                "Compressor A1",
                "Compressor",
                "SN-MP-SORT-001",
                "Workshop 1",
                EquipmentStatus.Working);

            await using (var writeContext = CreateContext(connection))
            {
                await writeContext.Equipment.AddAsync(equipment);
                await writeContext.MaintenancePlans.AddRangeAsync(
                    CreatePlan(equipment.Id, new DateOnly(2026, 12, 1)),
                    CreatePlan(equipment.Id, new DateOnly(2026, 11, 5)),
                    CreatePlan(equipment.Id, new DateOnly(2026, 11, 20)));
                await writeContext.SaveChangesAsync();
            }

            await using var readContext = CreateContext(connection);
            var repository = new MaintenancePlanRepository(readContext);

            var plans = await repository.GetAllAsync();

            Assert.Equal(
                new DateOnly[]
                {
                    new DateOnly(2026, 11, 5),
                    new DateOnly(2026, 11, 20),
                    new DateOnly(2026, 12, 1)
                },
                plans.Select(plan => plan.PlannedDate).ToArray());
        }

        [Fact]
        public async Task GetAllAsync_WithEquipmentFilter_ShouldReturnOnlyMatchingPlans()
        {
            await using var connection = await CreateOpenConnectionAsync();
            await MigrateAsync(connection);
            var requestedEquipment = new Equipment(
                "Compressor A1",
                "Compressor",
                "SN-MP-FILTER-001",
                "Workshop 1",
                EquipmentStatus.Working);
            var otherEquipment = new Equipment(
                "Pump B1",
                "Pump",
                "SN-MP-FILTER-002",
                "Workshop 2",
                EquipmentStatus.Working);

            await using (var writeContext = CreateContext(connection))
            {
                await writeContext.Equipment.AddRangeAsync(requestedEquipment, otherEquipment);
                await writeContext.MaintenancePlans.AddRangeAsync(
                    CreatePlan(requestedEquipment.Id, new DateOnly(2026, 11, 5)),
                    CreatePlan(otherEquipment.Id, new DateOnly(2026, 11, 6)));
                await writeContext.SaveChangesAsync();
            }

            await using var readContext = CreateContext(connection);
            var repository = new MaintenancePlanRepository(readContext);

            var plans = await repository.GetAllAsync(requestedEquipment.Id);

            Assert.Single(plans);
            Assert.Equal(requestedEquipment.Id, plans[0].EquipmentId);
        }

        private static MaintenancePlan CreatePlan(
            Guid equipmentId,
            DateOnly plannedDate)
        {
            return new MaintenancePlan(
                equipmentId,
                plannedDate,
                "Annual inspection",
                "Olena Kovalenko",
                "Check safety valves");
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
