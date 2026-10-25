using System.ComponentModel;

namespace EquipmentMonitoring.Application.DTOs.Threshold
{
    [Description("Допустимі межі показника. Потрібна хоча б одна межа; якщо задано обидві, MinValue ≤ MaxValue. Межі включні.")]
    public class UpsertThresholdDto
    {
        [Description("Мінімальне допустиме значення в канонічній одиниці типу (°C, V, %). Null — без нижньої межі.")]
        public double? MinValue { get; set; }

        [Description("Максимальне допустиме значення в канонічній одиниці типу (°C, V, %). Null — без верхньої межі.")]
        public double? MaxValue { get; set; }
    }
}
