using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Domain.Entities
{
    public class Measurement
    {
        public Guid Id { get; private set; }

        public Guid EquipmentId { get; private set; }

        public MeasurementType Type { get; private set; }

        public double Value { get; private set; }

        public string Unit { get; private set; } = string.Empty;

        public DateTime MeasuredAtUtc { get; private set; }

        public Equipment Equipment { get; private set; } = null!;

        public Measurement(
            Guid equipmentId,
            MeasurementType type,
            double value,
            string unit,
            DateTime measuredAtUtc)
        {
            Id = Guid.NewGuid();
            EquipmentId = equipmentId;
            Type = type;
            Value = value;
            Unit = unit;
            MeasuredAtUtc = measuredAtUtc;
        }
    }
}
