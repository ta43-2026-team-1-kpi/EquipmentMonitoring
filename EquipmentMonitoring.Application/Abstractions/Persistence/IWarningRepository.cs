using EquipmentMonitoring.Domain.Entities;

namespace EquipmentMonitoring.Application.Abstractions.Persistence
{
    public interface IWarningRepository
    {
        Task AddAsync(Warning warning, CancellationToken cancellationToken = default);

        Task<Warning?> FindAsync(Guid warningId, CancellationToken cancellationToken = default);

        Task<PagedList<Warning>> SearchAsync(
            WarningSearchCriteria criteria,
            CancellationToken cancellationToken = default);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
