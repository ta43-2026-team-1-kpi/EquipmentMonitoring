using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.DTOs.Equipment
{
    public class EquipmentDto
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Type { get; init; } = string.Empty;

        public string SerialNumber { get; init; } = string.Empty;

        public string Location { get; init; } = string.Empty;

        public EquipmentStatus Status { get; init; }
    }
}
