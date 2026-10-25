using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Domain.Entities
{
    public class Equipment
    {
        public Guid Id { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string Type { get; private set; } = string.Empty;

        public string SerialNumber { get; private set; } = string.Empty;

        public string Location { get; private set; } = string.Empty;

        public EquipmentStatus Status { get; private set; }

        public ICollection<Measurement> Measurements { get; private set; } = [];

        public ICollection<MaintenancePlan> MaintenancePlans { get; private set; } = [];

        public Equipment(string name, string type, string serialNumber, string location, EquipmentStatus status)
        {
            Id = Guid.NewGuid();
            Name = name;
            Type = type;
            SerialNumber = serialNumber;
            Location = location;
            Status = status;
        }
    }
}
