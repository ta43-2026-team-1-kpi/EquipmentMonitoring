using System.ComponentModel;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.DTOs.Threshold
{
    [Description("Допустимі межі показника.")]
    public class ThresholdDto
    {
        [Description("Ідентифікатор межі.")]
        public Guid Id { get; init; }

        [Description("Global — діє для всього обладнання; Equipment — лише для EquipmentId і має пріоритет над Global.")]
        public ThresholdScope Scope { get; init; }

        [Description("Обладнання, для якого задано межу. Null для глобальної межі.")]
        public Guid? EquipmentId { get; init; }

        [Description("Тип показника: Temperature, Voltage, Load.")]
        public MeasurementType MeasurementType { get; init; }

        [Description("Одиниця, у якій задано межі: °C, V або %.")]
        public string Unit { get; init; } = string.Empty;

        [Description("Мінімальне допустиме значення (включно). Null — без нижньої межі.")]
        public double? MinValue { get; init; }

        [Description("Максимальне допустиме значення (включно). Null — без верхньої межі.")]
        public double? MaxValue { get; init; }

        [Description("Час створення в UTC.")]
        public DateTimeOffset CreatedAtUtc { get; init; }

        [Description("Час останньої зміни меж в UTC.")]
        public DateTimeOffset UpdatedAtUtc { get; init; }
    }
}
