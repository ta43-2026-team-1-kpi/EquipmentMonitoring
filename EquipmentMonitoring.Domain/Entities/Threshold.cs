using EquipmentMonitoring.Domain.Common;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Domain.ValueObjects;

namespace EquipmentMonitoring.Domain.Entities
{
    public class Threshold
    {
        public Guid Id { get; private set; }

        /// <summary>
        /// Null for a global threshold that applies to every piece of equipment
        /// without its own threshold for the same measurement type.
        /// </summary>
        public Guid? EquipmentId { get; private set; }

        public MeasurementType MeasurementType { get; private set; }

        public double? MinValue { get; private set; }

        public double? MaxValue { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public DateTime UpdatedAtUtc { get; private set; }

        public Equipment? Equipment { get; private set; }

        public ThresholdScope Scope => EquipmentId is null ? ThresholdScope.Global : ThresholdScope.Equipment;

        private Threshold()
        {
        }

        private Threshold(Guid? equipmentId, MeasurementType measurementType, double? minValue, double? maxValue, DateTime nowUtc)
        {
            Guard.Defined(measurementType, nameof(measurementType));
            Guard.Utc(nowUtc, nameof(nowUtc));
            EnsureValidLimits(minValue, maxValue);

            Id = Guid.NewGuid();
            EquipmentId = equipmentId;
            MeasurementType = measurementType;
            MinValue = minValue;
            MaxValue = maxValue;
            CreatedAtUtc = nowUtc;
            UpdatedAtUtc = nowUtc;
        }

        public static Threshold CreateGlobal(MeasurementType measurementType, double? minValue, double? maxValue, DateTime nowUtc)
        {
            return new Threshold(null, measurementType, minValue, maxValue, nowUtc);
        }

        public static Threshold CreateForEquipment(Guid equipmentId, MeasurementType measurementType, double? minValue, double? maxValue, DateTime nowUtc)
        {
            Guard.NotEmpty(equipmentId, nameof(equipmentId));

            return new Threshold(equipmentId, measurementType, minValue, maxValue, nowUtc);
        }

        public void ChangeLimits(double? minValue, double? maxValue, DateTime nowUtc)
        {
            Guard.Utc(nowUtc, nameof(nowUtc));
            EnsureValidLimits(minValue, maxValue);

            MinValue = minValue;
            MaxValue = maxValue;
            UpdatedAtUtc = nowUtc;
        }

        public bool AppliesTo(Guid equipmentId, MeasurementType measurementType)
        {
            return MeasurementType == measurementType
                && (EquipmentId is null || EquipmentId == equipmentId);
        }

        /// <remarks>Limits are inclusive: a value equal to a limit is not a breach.</remarks>
        public ThresholdBreach? Evaluate(double value)
        {
            Guard.Finite(value, nameof(value));

            if (MinValue is not null && value < MinValue.Value)
            {
                return new ThresholdBreach(ThresholdBreachType.BelowMinimum, MinValue.Value);
            }

            if (MaxValue is not null && value > MaxValue.Value)
            {
                return new ThresholdBreach(ThresholdBreachType.AboveMaximum, MaxValue.Value);
            }

            return null;
        }

        private static void EnsureValidLimits(double? minValue, double? maxValue)
        {
            Guard.Finite(minValue, nameof(minValue));
            Guard.Finite(maxValue, nameof(maxValue));

            if (minValue is null && maxValue is null)
            {
                throw new ArgumentException("At least one limit must be specified.", nameof(minValue));
            }

            if (minValue > maxValue)
            {
                throw new ArgumentException("Minimum must not exceed maximum.", nameof(minValue));
            }
        }
    }
}
