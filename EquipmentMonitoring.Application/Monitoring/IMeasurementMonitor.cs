using EquipmentMonitoring.Domain.Entities;

namespace EquipmentMonitoring.Application.Monitoring
{
    public interface IMeasurementMonitor
    {
        /// <summary>
        /// Runs all measurement rules and stages resulting warnings in the current unit of work.
        /// The caller commits them together with the measurement.
        /// </summary>
        Task<IReadOnlyList<Warning>> InspectAsync(Measurement measurement, CancellationToken cancellationToken = default);
    }
}
