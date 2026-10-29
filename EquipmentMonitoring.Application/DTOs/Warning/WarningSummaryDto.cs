using System.ComponentModel;

namespace EquipmentMonitoring.Application.DTOs.Warning
{
    [Description("Зведення активних попереджень за обладнанням.")]
    public class WarningSummaryDto
    {
        [Description("Загальна кількість активних попереджень.")]
        public int TotalActive { get; init; }

        [Description("Обладнання з активними попередженнями; спочатку те, де їх найбільше.")]
        public IReadOnlyList<EquipmentWarningSummaryDto> Equipment { get; init; } = [];
    }
}
