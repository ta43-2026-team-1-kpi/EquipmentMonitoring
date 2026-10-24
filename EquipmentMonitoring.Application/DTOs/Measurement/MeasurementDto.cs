using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.DTOs.Measurement
{
    public class MeasurementDto
    {
        public Guid Id { get; init; }

        public Guid EquipmentId { get; init; }

        public MeasurementType Type { get; init; }

        public double Value { get; init; }

        public string Unit { get; init; } = string.Empty;

        public DateTimeOffset MeasuredAtUtc { get; init; }
    }
}
