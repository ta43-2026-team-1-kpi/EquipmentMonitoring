namespace EquipmentMonitoring.Application.Exceptions
{
    public class MaintenancePlanEquipmentNotFoundException : Exception
    {
        public MaintenancePlanEquipmentNotFoundException(
            Guid equipmentId,
            Exception? innerException = null)
            : base(
                $"The equipment '{equipmentId}' does not exist for this maintenance plan.",
                innerException)
        {
            EquipmentId = equipmentId;
        }

        public Guid EquipmentId { get; }
    }
}
