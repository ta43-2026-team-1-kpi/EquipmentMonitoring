using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Domain.Entities;

namespace EquipmentMonitoring.Tests.TestDoubles
{
    internal sealed class InMemoryWarningRepository : IWarningRepository
    {
        public List<Warning> Added { get; } = [];

        public int SaveChangesCallsCount { get; private set; }

        public Task AddAsync(Warning warning, CancellationToken cancellationToken = default)
        {
            Added.Add(warning);
            return Task.CompletedTask;
        }

        public Task<Warning?> FindAsync(Guid warningId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Added.SingleOrDefault(w => w.Id == warningId));
        }

        public Task<PagedList<Warning>> SearchAsync(WarningSearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<ActiveWarningCount>> CountActiveByEquipmentAndTypeAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallsCount++;
            return Task.FromResult(Added.Count);
        }
    }
}
