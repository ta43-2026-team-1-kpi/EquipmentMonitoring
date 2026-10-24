using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.Abstractions.Persistence
{
    public interface IMeasurementRepository
    {
        Task AddAsync(Measurement measurement, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Measurement>> GetByEquipmentAsync(
            Guid equipmentId,
            MeasurementType? type,
            DateTime? fromUtc,
            DateTime? toUtc,
            CancellationToken cancellationToken = default);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
