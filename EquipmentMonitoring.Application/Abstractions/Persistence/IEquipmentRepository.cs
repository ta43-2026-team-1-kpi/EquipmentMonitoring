using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.Abstractions.Persistence
{
    public interface IEquipmentRepository
    {
        Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Equipment>> GetAllAsync(
            EquipmentStatus? status = null,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsBySerialNumberAsync(
            string serialNumber,
            CancellationToken cancellationToken = default);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
