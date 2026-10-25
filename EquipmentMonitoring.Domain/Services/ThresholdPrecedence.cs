using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Domain.Services
{
    /// <summary>
    /// An equipment's own threshold fully replaces the global one for the same
    /// measurement type, even when it is looser.
    /// </summary>
    public static class ThresholdPrecedence
    {
        public static Threshold? SelectEffective(
            IEnumerable<Threshold> candidates,
            Guid equipmentId,
            MeasurementType measurementType)
        {
            Threshold? global = null;

            foreach (var threshold in candidates)
            {
                if (!threshold.AppliesTo(equipmentId, measurementType))
                {
                    continue;
                }

                if (threshold.Scope == ThresholdScope.Equipment)
                {
                    return threshold;
                }

                global ??= threshold;
            }

            return global;
        }

        public static IReadOnlyList<Threshold> SelectEffectiveForEachType(
            IEnumerable<Threshold> candidates,
            Guid equipmentId)
        {
            var thresholds = candidates as IReadOnlyCollection<Threshold> ?? candidates.ToList();

            return Enum.GetValues<MeasurementType>()
                .Select(type => SelectEffective(thresholds, equipmentId, type))
                .OfType<Threshold>()
                .ToList();
        }
    }
}
