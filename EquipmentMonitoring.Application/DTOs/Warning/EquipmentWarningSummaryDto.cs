using System.ComponentModel;

namespace EquipmentMonitoring.Application.DTOs.Warning
{
    [Description("Активні попередження одного обладнання.")]
    public class EquipmentWarningSummaryDto
    {
        [Description("Ідентифікатор обладнання.")]
        public Guid EquipmentId { get; init; }

        [Description("Назва обладнання.")]
        public string EquipmentName { get; init; } = string.Empty;

        [Description("Серійний номер обладнання.")]
        public string EquipmentSerialNumber { get; init; } = string.Empty;

        [Description("Кількість активних попереджень.")]
        public int ActiveCount { get; init; }

        [Description("Час створення останнього активного попередження в UTC.")]
        public DateTimeOffset LatestCreatedAtUtc { get; init; }

        [Description("Кількість активних попереджень за типами показників; спочатку найчастіший.")]
        public IReadOnlyList<MeasurementTypeWarningCountDto> ByMeasurementType { get; init; } = [];
    }
}
