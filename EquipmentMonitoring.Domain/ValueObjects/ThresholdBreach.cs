using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Domain.ValueObjects
{
    public sealed record ThresholdBreach(ThresholdBreachType Type, double Limit);
}
