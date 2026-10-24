using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.DTOs.Equipment
{
    public class CreateEquipmentDto
    {
        public string? Name { get; set; }

        public string? Type { get; set; }

        public string? SerialNumber { get; set; }

        public string? Location { get; set; }

        public EquipmentStatus? Status { get; set; }
    }
}
