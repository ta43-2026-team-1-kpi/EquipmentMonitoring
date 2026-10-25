using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Tests.TestDoubles
{
    internal sealed class InMemoryThresholdRepository : IThresholdRepository
    {
        public List<Threshold> Thresholds { get; } = [];

        public int SaveChangesCallsCount { get; private set; }

        public Exception? SaveChangesException { get; set; }

        public Task AddAsync(Threshold threshold, CancellationToken cancellationToken = default)
        {
            Thresholds.Add(threshold);
            return Task.CompletedTask;
        }

        public void Remove(Threshold threshold)
        {
            Thresholds.Remove(threshold);
        }

        public Task<Threshold?> FindAsync(Guid? equipmentId, MeasurementType measurementType, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Thresholds.SingleOrDefault(t => t.EquipmentId == equipmentId && t.MeasurementType == measurementType));
        }

        public Task<IReadOnlyList<Threshold>> ListAsync(Guid? equipmentId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Threshold> result = Thresholds.Where(t => t.EquipmentId == equipmentId).OrderBy(t => t.MeasurementType).ToList();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<Threshold>> ListApplicableAsync(Guid equipmentId, MeasurementType? measurementType, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Threshold> result = Thresholds
                .Where(t => t.EquipmentId == equipmentId || t.EquipmentId == null)
                .Where(t => measurementType is null || t.MeasurementType == measurementType)
                .ToList();
            return Task.FromResult(result);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallsCount++;

            if (SaveChangesException is not null)
            {
                throw SaveChangesException;
            }

            return Task.FromResult(1);
        }
    }
}
