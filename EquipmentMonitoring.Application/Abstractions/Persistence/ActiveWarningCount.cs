using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.Abstractions.Persistence
{
    public sealed record ActiveWarningCount(
        Guid EquipmentId,
        string EquipmentName,
        string EquipmentSerialNumber,
        MeasurementType MeasurementType,
        int Count,
        DateTime LatestCreatedAtUtc);
}
