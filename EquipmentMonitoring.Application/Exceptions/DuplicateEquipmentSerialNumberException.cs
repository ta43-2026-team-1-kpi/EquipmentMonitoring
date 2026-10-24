namespace EquipmentMonitoring.Application.Exceptions
{
    public class DuplicateEquipmentSerialNumberException : Exception
    {
        public DuplicateEquipmentSerialNumberException(string serialNumber)
            : base($"Обладнання з серійним номером '{serialNumber}' вже існує.")
        {
            SerialNumber = serialNumber;
        }

        public DuplicateEquipmentSerialNumberException(string serialNumber, Exception innerException)
            : base($"Обладнання з серійним номером '{serialNumber}' вже існує.", innerException)
        {
            SerialNumber = serialNumber;
        }

        public string SerialNumber { get; }
    }
}
