using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.DTOs.Warning
{
    public class WarningDto
    {
        public Guid Id { get; init; }

        public Guid EquipmentId { get; init; }

        public string EquipmentName { get; init; } = string.Empty;

        public string EquipmentSerialNumber { get; init; } = string.Empty;

        public Guid MeasurementId { get; init; }

        public MeasurementType MeasurementType { get; init; }

        public double ActualValue { get; init; }

        public string Unit { get; init; } = string.Empty;

        public ThresholdBreachType BreachType { get; init; }

        public double LimitValue { get; init; }

        public Guid ThresholdId { get; init; }

        public ThresholdScope ThresholdScope { get; init; }

        public DateTimeOffset MeasuredAtUtc { get; init; }

        public DateTimeOffset CreatedAtUtc { get; init; }

        public WarningStatus Status { get; init; }

        public DateTimeOffset? AcknowledgedAtUtc { get; init; }
    }
}
