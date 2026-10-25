using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence.Repositories
{
    public class ThresholdRepository : IThresholdRepository
    {
        private readonly EquipmentMonitoringDbContext _dbContext;

        public ThresholdRepository(EquipmentMonitoringDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Threshold threshold, CancellationToken cancellationToken = default)
        {
            await _dbContext.Thresholds.AddAsync(threshold, cancellationToken);
        }

        public void Remove(Threshold threshold)
        {
            _dbContext.Thresholds.Remove(threshold);
        }

        public Task<Threshold?> FindAsync(
            Guid? equipmentId,
            MeasurementType measurementType,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.Thresholds.SingleOrDefaultAsync(
                t => t.EquipmentId == equipmentId && t.MeasurementType == measurementType,
                cancellationToken);
        }

        public async Task<IReadOnlyList<Threshold>> ListAsync(
            Guid? equipmentId,
            CancellationToken cancellationToken = default)
        {
            var thresholds = await _dbContext.Thresholds
                .AsNoTracking()
                .Where(t => t.EquipmentId == equipmentId)
                .ToListAsync(cancellationToken);

            return thresholds.OrderBy(t => t.MeasurementType).ToList();
        }

        public async Task<IReadOnlyList<Threshold>> ListApplicableAsync(
            Guid equipmentId,
            MeasurementType? measurementType,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Thresholds
                .AsNoTracking()
                .Where(t => t.EquipmentId == equipmentId || t.EquipmentId == null);

            if (measurementType is not null)
            {
                query = query.Where(t => t.MeasurementType == measurementType.Value);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (SqliteErrors.IsUniqueViolation(exception))
            {
                var threshold = FindFailedThreshold(exception);
                throw new ThresholdConflictException(threshold?.EquipmentId, threshold?.MeasurementType ?? default, exception);
            }
            catch (DbUpdateException exception) when (SqliteErrors.IsForeignKeyViolation(exception))
            {
                var threshold = FindFailedThreshold(exception);
                throw new EquipmentNotFoundException(threshold?.EquipmentId ?? Guid.Empty, exception);
            }
        }

        private Threshold? FindFailedThreshold(DbUpdateException exception)
        {
            return exception.Entries.Select(entry => entry.Entity).OfType<Threshold>().FirstOrDefault()
                ?? _dbContext.ChangeTracker.Entries<Threshold>()
                    .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
                    .Select(entry => entry.Entity)
                    .FirstOrDefault();
        }
    }
}
