using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence.Repositories
{
    public class MeasurementRepository : IMeasurementRepository
    {
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
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
