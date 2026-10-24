using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EquipmentMonitoring.Infrastructure.Persistence.Repositories
{
    public class EquipmentRepository : IEquipmentRepository
    {
        private readonly EquipmentMonitoringDbContext _dbContext;

        public EquipmentRepository(EquipmentMonitoringDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default)
        {
            await _dbContext.Equipment.AddAsync(equipment, cancellationToken);
        }

        public async Task<IReadOnlyList<Equipment>> GetAllAsync(
            EquipmentStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Equipment> query = _dbContext.Equipment.AsNoTracking();

            if (status is not null)
            {
                query = query.Where(e => e.Status == status.Value);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
