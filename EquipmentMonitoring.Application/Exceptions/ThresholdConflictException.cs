using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.Exceptions
{
    /// <summary>
    /// Thrown when a concurrent request created the same threshold first.
    /// </summary>
    public class ThresholdConflictException : Exception
    {
        public ThresholdConflictException(Guid? equipmentId, MeasurementType measurementType, Exception innerException)
            : base($"Межу для показника {measurementType} щойно змінено іншим запитом. Повторіть запит.", innerException)
        {
            EquipmentId = equipmentId;
            MeasurementType = measurementType;
        }

        public Guid? EquipmentId { get; }

        public MeasurementType MeasurementType { get; }
    }
}
