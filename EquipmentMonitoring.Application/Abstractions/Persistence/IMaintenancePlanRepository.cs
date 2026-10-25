using EquipmentMonitoring.Domain.Entities;

namespace EquipmentMonitoring.Application.Abstractions.Persistence
{
    public interface IMaintenancePlanRepository
    {
        Task AddAsync(
            MaintenancePlan maintenancePlan,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<MaintenancePlan>> GetAllAsync(
            Guid? equipmentId = null,
            CancellationToken cancellationToken = default);

        Task<MaintenancePlan?> GetByIdAsync(
            Guid maintenancePlanId,
            CancellationToken cancellationToken = default);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
