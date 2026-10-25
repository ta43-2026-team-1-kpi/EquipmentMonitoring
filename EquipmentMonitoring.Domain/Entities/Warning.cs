using EquipmentMonitoring.Domain.Common;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Domain.ValueObjects;

namespace EquipmentMonitoring.Domain.Entities
{
    /// <summary>
    /// Immutable record of a threshold breach. Limit and measurement data are copied
    /// at creation time so the warning stays accurate after thresholds are changed or removed.
    /// </summary>
    public class Warning
    {
        public Guid Id { get; private set; }

        public Guid EquipmentId { get; private set; }

        public Guid MeasurementId { get; private set; }

        public Guid ThresholdId { get; private set; }

        public ThresholdScope ThresholdScope { get; private set; }

        public MeasurementType MeasurementType { get; private set; }

        public double ActualValue { get; private set; }

        public string Unit { get; private set; } = string.Empty;

        public ThresholdBreachType BreachType { get; private set; }

        public double LimitValue { get; private set; }

        public DateTime MeasuredAtUtc { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public WarningStatus Status { get; private set; }

        public DateTime? AcknowledgedAtUtc { get; private set; }

        public Equipment Equipment { get; private set; } = null!;

        public Measurement Measurement { get; private set; } = null!;

        private Warning()
        {
        }

        public static Warning FromBreach(Measurement measurement, Threshold threshold, ThresholdBreach breach, DateTime nowUtc)
        {
            ArgumentNullException.ThrowIfNull(measurement);
            ArgumentNullException.ThrowIfNull(threshold);
            ArgumentNullException.ThrowIfNull(breach);
            Guard.Utc(nowUtc, nameof(nowUtc));

            if (!threshold.AppliesTo(measurement.EquipmentId, measurement.Type))
            {
                throw new ArgumentException("Threshold does not apply to the measurement.", nameof(threshold));
            }

            return new Warning
            {
                Id = Guid.NewGuid(),
                EquipmentId = measurement.EquipmentId,
                MeasurementId = measurement.Id,
                ThresholdId = threshold.Id,
                ThresholdScope = threshold.Scope,
                MeasurementType = measurement.Type,
                ActualValue = measurement.Value,
                Unit = measurement.Unit,
                BreachType = breach.Type,
                LimitValue = breach.Limit,
                MeasuredAtUtc = measurement.MeasuredAtUtc,
                CreatedAtUtc = nowUtc,
                Status = WarningStatus.Active
            };
        }

        /// <returns><c>false</c> if the warning was already acknowledged.</returns>
        public bool Acknowledge(DateTime nowUtc)
        {
            Guard.Utc(nowUtc, nameof(nowUtc));

            if (Status == WarningStatus.Acknowledged)
            {
                return false;
            }

            Status = WarningStatus.Acknowledged;
            AcknowledgedAtUtc = nowUtc;
            return true;
        }
    }
}
