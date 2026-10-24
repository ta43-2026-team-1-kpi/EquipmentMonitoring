using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence.Repositories
{
    public class MeasurementRepository : IMeasurementRepository
    {
        private const int SqliteConstraintForeignKeyExtendedErrorCode = 787;

        private readonly EquipmentMonitoringDbContext _dbContext;

        public MeasurementRepository(EquipmentMonitoringDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(
            Measurement measurement,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.Measurements.AddAsync(measurement, cancellationToken);
        }

        public async Task<IReadOnlyList<Measurement>> GetByEquipmentAsync(
            Guid equipmentId,
            MeasurementType? type,
            DateTime? fromUtc,
            DateTime? toUtc,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Measurement> query = _dbContext.Measurements
                .AsNoTracking()
                .Where(m => m.EquipmentId == equipmentId);

            if (type is not null)
            {
                query = query.Where(m => m.Type == type.Value);
            }

            if (fromUtc is not null)
            {
                query = query.Where(m => m.MeasuredAtUtc >= fromUtc.Value);
            }

            if (toUtc is not null)
            {
                query = query.Where(m => m.MeasuredAtUtc <= toUtc.Value);
            }

            return await query
                .OrderByDescending(m => m.MeasuredAtUtc)
                .ThenBy(m => m.Id)
                .ToListAsync(cancellationToken);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return SaveChangesInternalAsync(cancellationToken);
        }

        private async Task<int> SaveChangesInternalAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
                when (IsMeasurementEquipmentForeignKeyViolation(exception))
            {
                var equipmentId = exception.Entries
                    .Select(entry => entry.Entity)
                    .OfType<Measurement>()
                    .Select(measurement => measurement.EquipmentId)
                    .FirstOrDefault();

                throw new MeasurementEquipmentNotFoundException(equipmentId, exception);
            }
        }

        private static bool IsMeasurementEquipmentForeignKeyViolation(DbUpdateException exception)
        {
            if (exception.InnerException is not SqliteException sqliteException)
            {
                return false;
            }

            if (sqliteException.SqliteExtendedErrorCode
                != SqliteConstraintForeignKeyExtendedErrorCode)
            {
                return false;
            }

            return sqliteException.Message.Contains("FOREIGN KEY constraint failed", StringComparison.Ordinal)
                || sqliteException.Message.Contains("FK_Measurements_Equipment_EquipmentId", StringComparison.Ordinal);
        }
    }
}
