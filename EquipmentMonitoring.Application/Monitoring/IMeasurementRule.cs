using EquipmentMonitoring.Domain.Entities;

namespace EquipmentMonitoring.Application.Monitoring
{
    /// <summary>
    /// A check applied to every newly recorded measurement.
    /// Implementations must not persist anything: returned warnings are saved
    /// together with the measurement by the caller.
    /// </summary>
    public interface IMeasurementRule
    {
        Task<IReadOnlyList<Warning>> EvaluateAsync(Measurement measurement, CancellationToken cancellationToken = default);
    }
}
