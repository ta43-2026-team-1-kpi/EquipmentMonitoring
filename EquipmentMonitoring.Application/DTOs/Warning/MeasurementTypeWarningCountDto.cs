using System.ComponentModel;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.DTOs.Warning
{
    [Description("Кількість активних попереджень за типом показника.")]
    public class MeasurementTypeWarningCountDto
    {
        [Description("Тип показника: Temperature, Voltage, Load.")]
        public MeasurementType MeasurementType { get; init; }

        [Description("Кількість активних попереджень.")]
        public int Count { get; init; }
    }
}
