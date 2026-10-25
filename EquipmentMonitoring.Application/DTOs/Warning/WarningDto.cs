using System.ComponentModel;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.DTOs.Warning
{
    [Description("Попередження про вихід показника за допустимі межі. Значення межі зафіксовано на момент створення.")]
    public class WarningDto
    {
        [Description("Ідентифікатор попередження.")]
        public Guid Id { get; init; }

        [Description("Ідентифікатор обладнання.")]
        public Guid EquipmentId { get; init; }

        [Description("Назва обладнання.")]
        public string EquipmentName { get; init; } = string.Empty;

        [Description("Серійний номер обладнання.")]
        public string EquipmentSerialNumber { get; init; } = string.Empty;

        [Description("Вимірювання, яке спричинило попередження.")]
        public Guid MeasurementId { get; init; }

        [Description("Тип показника: Temperature, Voltage, Load.")]
        public MeasurementType MeasurementType { get; init; }

        [Description("Фактичне значення показника.")]
        public double ActualValue { get; init; }

        [Description("Одиниця вимірювання: °C, V або %.")]
        public string Unit { get; init; } = string.Empty;

        [Description("BelowMinimum — нижче мінімуму; AboveMaximum — вище максимуму.")]
        public ThresholdBreachType BreachType { get; init; }

        [Description("Порушена межа на момент створення попередження.")]
        public double LimitValue { get; init; }

        [Description("Ідентифікатор межі, яку було порушено.")]
        public Guid ThresholdId { get; init; }

        [Description("Рівень межі, яку було порушено: Global або Equipment.")]
        public ThresholdScope ThresholdScope { get; init; }

        [Description("Час вимірювання в UTC.")]
        public DateTimeOffset MeasuredAtUtc { get; init; }

        [Description("Час створення попередження в UTC.")]
        public DateTimeOffset CreatedAtUtc { get; init; }

        [Description("Active — потребує уваги; Acknowledged — опрацьовано.")]
        public WarningStatus Status { get; init; }

        [Description("Час позначення як опрацьованого в UTC. Null для активних.")]
        public DateTimeOffset? AcknowledgedAtUtc { get; init; }
    }
}
