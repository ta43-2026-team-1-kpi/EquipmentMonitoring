namespace EquipmentMonitoring.Application.Exceptions
{
    public class EquipmentNotFoundException : Exception
    {
        public EquipmentNotFoundException(Guid equipmentId)
            : base($"Обладнання з ідентифікатором '{equipmentId}' не знайдено.")
        {
            EquipmentId = equipmentId;
        }

        public EquipmentNotFoundException(Guid equipmentId, Exception innerException)
            : base($"Обладнання з ідентифікатором '{equipmentId}' не знайдено.", innerException)
        {
            EquipmentId = equipmentId;
        }

        public Guid EquipmentId { get; }
    }
}
