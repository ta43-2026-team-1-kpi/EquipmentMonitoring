using EquipmentMonitoring.Application.Abstractions.Persistence;
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

        public static WarningSummaryDto ToSummaryDto(this IEnumerable<ActiveWarningCount> counts)
        {
            var equipment = counts
                .GroupBy(c => c.EquipmentId)
                .Select(g => new EquipmentWarningSummaryDto
                {
                    EquipmentId = g.Key,
                    EquipmentName = g.First().EquipmentName,
                    EquipmentSerialNumber = g.First().EquipmentSerialNumber,
                    ActiveCount = g.Sum(c => c.Count),
                    LatestCreatedAtUtc = AsUtcOffset(g.Max(c => c.LatestCreatedAtUtc)),
                    ByMeasurementType = g
                        .OrderByDescending(c => c.Count)
                        .ThenBy(c => c.MeasurementType)
                        .Select(c => new MeasurementTypeWarningCountDto { MeasurementType = c.MeasurementType, Count = c.Count })
                        .ToList()
                })
                .OrderByDescending(e => e.ActiveCount)
                .ThenByDescending(e => e.LatestCreatedAtUtc)
                .ThenBy(e => e.EquipmentId)
                .ToList();

            return new WarningSummaryDto
            {
                TotalActive = equipment.Sum(e => e.ActiveCount),
                Equipment = equipment
            };
        }

        private static DateTimeOffset AsUtcOffset(DateTime value)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc), TimeSpan.Zero);
        }
    }
}
