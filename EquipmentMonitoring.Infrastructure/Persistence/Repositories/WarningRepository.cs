using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence.Repositories
{
    public class WarningRepository : IWarningRepository
    {
        private readonly EquipmentMonitoringDbContext _dbContext;

        public WarningRepository(EquipmentMonitoringDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Warning warning, CancellationToken cancellationToken = default)
        {
            await _dbContext.Warnings.AddAsync(warning, cancellationToken);
        }

        public Task<Warning?> FindAsync(Guid warningId, CancellationToken cancellationToken = default)
        {
            return _dbContext.Warnings
                .Include(w => w.Equipment)
                .SingleOrDefaultAsync(w => w.Id == warningId, cancellationToken);
        }

        public async Task<PagedList<Warning>> SearchAsync(
            WarningSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(criteria.Page, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(criteria.PageSize, 1);

            IQueryable<Warning> query = _dbContext.Warnings.AsNoTracking();

            if (criteria.EquipmentId is not null)
            {
                query = query.Where(w => w.EquipmentId == criteria.EquipmentId.Value);
            }

            if (criteria.Status is not null)
            {
                query = query.Where(w => w.Status == criteria.Status.Value);
            }

            if (criteria.MeasurementType is not null)
            {
                query = query.Where(w => w.MeasurementType == criteria.MeasurementType.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Include(w => w.Equipment)
                .OrderByDescending(w => w.CreatedAtUtc)
                .ThenBy(w => w.Id)
                .Skip((criteria.Page - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedList<Warning>(items, totalCount);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                foreach (var entry in exception.Entries)
                {
                    await entry.ReloadAsync(cancellationToken);
                }

                throw new ConcurrencyConflictException(exception);
            }
        }
    }
}
