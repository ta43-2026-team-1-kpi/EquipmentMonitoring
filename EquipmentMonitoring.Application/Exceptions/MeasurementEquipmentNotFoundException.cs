namespace EquipmentMonitoring.Application.Exceptions
{
    public class MeasurementEquipmentNotFoundException : Exception
    {
        public MeasurementEquipmentNotFoundException(Guid equipmentId)
            : base($"Обладнання з ідентифікатором '{equipmentId}' не знайдено.")
        {
            EquipmentId = equipmentId;
        }

        public MeasurementEquipmentNotFoundException(Guid equipmentId, Exception innerException)
            : base($"Обладнання з ідентифікатором '{equipmentId}' не знайдено.", innerException)
        {
            EquipmentId = equipmentId;
        }

        public Guid EquipmentId { get; }
    }
}
