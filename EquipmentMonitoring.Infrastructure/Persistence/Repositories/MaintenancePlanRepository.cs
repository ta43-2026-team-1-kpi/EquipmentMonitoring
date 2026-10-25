using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence.Repositories
{
    public class MaintenancePlanRepository : IMaintenancePlanRepository
    {
        private const int SqliteConstraintForeignKeyExtendedErrorCode = 787;

        private readonly EquipmentMonitoringDbContext _dbContext;

        public MaintenancePlanRepository(EquipmentMonitoringDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(
            MaintenancePlan maintenancePlan,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.MaintenancePlans.AddAsync(
                maintenancePlan,
                cancellationToken);
        }

        public async Task<IReadOnlyList<MaintenancePlan>> GetAllAsync(
            Guid? equipmentId = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<MaintenancePlan> query =
                _dbContext.MaintenancePlans.AsNoTracking();

            if (equipmentId is not null)
            {
                query = query.Where(plan => plan.EquipmentId == equipmentId.Value);
            }

            return await query
                .OrderBy(plan => plan.PlannedDate)
                .ThenBy(plan => plan.Id)
                .ToListAsync(cancellationToken);
        }

        public Task<MaintenancePlan?> GetByIdAsync(
            Guid maintenancePlanId,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.MaintenancePlans
                .SingleOrDefaultAsync(
                    plan => plan.Id == maintenancePlanId,
                    cancellationToken);
        }

        public async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
                when (IsEquipmentForeignKeyViolation(exception))
            {
                var equipmentId = exception.Entries
                    .Select(entry => entry.Entity)
                    .OfType<MaintenancePlan>()
                    .Select(plan => plan.EquipmentId)
                    .FirstOrDefault();

                throw new MaintenancePlanEquipmentNotFoundException(
                    equipmentId,
                    exception);
            }
        }

        private static bool IsEquipmentForeignKeyViolation(
            DbUpdateException exception)
        {
            if (exception.InnerException is not SqliteException sqliteException
                || sqliteException.SqliteExtendedErrorCode
                    != SqliteConstraintForeignKeyExtendedErrorCode)
            {
                return false;
            }

            return sqliteException.Message.Contains(
                    "FOREIGN KEY constraint failed",
                    StringComparison.Ordinal)
                || sqliteException.Message.Contains(
                    "FK_MaintenancePlans_Equipment_EquipmentId",
                    StringComparison.Ordinal);
        }
    }
}
