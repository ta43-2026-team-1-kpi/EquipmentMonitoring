namespace EquipmentMonitoring.Domain.Enums
{
    public static class MeasurementTypeExtensions
    {
        public static string GetUnit(this MeasurementType type)
        {
            return type switch
            {
                MeasurementType.Temperature => "°C",
                MeasurementType.Voltage => "V",
                MeasurementType.Load => "%",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Undefined MeasurementType value.")
            };
        }
    }
}
