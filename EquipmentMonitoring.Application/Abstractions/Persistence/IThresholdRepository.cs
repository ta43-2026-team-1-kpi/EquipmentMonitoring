using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.Abstractions.Persistence
{
    public interface IThresholdRepository
    {
        Task AddAsync(Threshold threshold, CancellationToken cancellationToken = default);

        void Remove(Threshold threshold);

        /// <param name="equipmentId">Null to look up the global threshold.</param>
        Task<Threshold?> FindAsync(
            Guid? equipmentId,
            MeasurementType measurementType,
            CancellationToken cancellationToken = default);

        /// <param name="equipmentId">Null to list global thresholds.</param>
        Task<IReadOnlyList<Threshold>> ListAsync(
            Guid? equipmentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns both the equipment's own thresholds and the global ones.
        /// Precedence is resolved by the caller.
        /// </summary>
        Task<IReadOnlyList<Threshold>> ListApplicableAsync(
            Guid equipmentId,
            MeasurementType? measurementType,
            CancellationToken cancellationToken = default);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
