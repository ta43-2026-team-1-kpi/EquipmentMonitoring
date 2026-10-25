using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Tests.TestDoubles
{
    internal sealed class StubEquipmentRepository : IEquipmentRepository
    {
        public HashSet<Guid> ExistingIds { get; } = [];

        public Task<bool> ExistsByIdAsync(Guid equipmentId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ExistingIds.Contains(equipmentId));
        }

        public Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<Equipment>> GetAllAsync(EquipmentStatus? status = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> ExistsBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
