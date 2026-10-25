using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.DTOs.Threshold
{
    public class ThresholdDto
    {
        public Guid Id { get; init; }

        public ThresholdScope Scope { get; init; }

        public Guid? EquipmentId { get; init; }

        public MeasurementType MeasurementType { get; init; }

        public string Unit { get; init; } = string.Empty;

        public double? MinValue { get; init; }

        public double? MaxValue { get; init; }

        public DateTimeOffset CreatedAtUtc { get; init; }

        public DateTimeOffset UpdatedAtUtc { get; init; }
    }
}
