using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.Abstractions.Persistence
{
    public sealed record WarningSearchCriteria(
        Guid? EquipmentId,
        WarningStatus? Status,
        MeasurementType? MeasurementType,
        int Page,
        int PageSize);
}
