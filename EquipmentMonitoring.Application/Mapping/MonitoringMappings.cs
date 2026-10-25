using EquipmentMonitoring.Application.DTOs.Threshold;
using EquipmentMonitoring.Application.DTOs.Warning;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.Mapping
{
    public static class MonitoringMappings
    {
        public static ThresholdDto ToDto(this Threshold threshold)
        {
            return new ThresholdDto
            {
                Id = threshold.Id,
                Scope = threshold.Scope,
                EquipmentId = threshold.EquipmentId,
                MeasurementType = threshold.MeasurementType,
                Unit = threshold.MeasurementType.GetUnit(),
                MinValue = threshold.MinValue,
                MaxValue = threshold.MaxValue,
                CreatedAtUtc = AsUtcOffset(threshold.CreatedAtUtc),
                UpdatedAtUtc = AsUtcOffset(threshold.UpdatedAtUtc)
            };
        }

        /// <remarks>Requires <see cref="Warning.Equipment"/> to be loaded.</remarks>
        public static WarningDto ToDto(this Warning warning)
        {
            return new WarningDto
            {
                Id = warning.Id,
                EquipmentId = warning.EquipmentId,
                EquipmentName = warning.Equipment.Name,
                EquipmentSerialNumber = warning.Equipment.SerialNumber,
                MeasurementId = warning.MeasurementId,
                MeasurementType = warning.MeasurementType,
                ActualValue = warning.ActualValue,
                Unit = warning.Unit,
                BreachType = warning.BreachType,
                LimitValue = warning.LimitValue,
                ThresholdId = warning.ThresholdId,
                ThresholdScope = warning.ThresholdScope,
                MeasuredAtUtc = AsUtcOffset(warning.MeasuredAtUtc),
                CreatedAtUtc = AsUtcOffset(warning.CreatedAtUtc),
                Status = warning.Status,
                AcknowledgedAtUtc = warning.AcknowledgedAtUtc is null ? null : AsUtcOffset(warning.AcknowledgedAtUtc.Value)
            };
        }

        private static DateTimeOffset AsUtcOffset(DateTime value)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc), TimeSpan.Zero);
        }
    }
}
