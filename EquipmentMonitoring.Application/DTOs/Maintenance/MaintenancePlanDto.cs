using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.DTOs.Maintenance
{
    public class MaintenancePlanDto
    {
        public Guid Id { get; set; }

        public Guid EquipmentId { get; set; }

        public DateOnly PlannedDate { get; set; }

        public string WorkType { get; set; } = string.Empty;

        public string ResponsibleEmployee { get; set; } = string.Empty;

        public string? Note { get; set; }

        public MaintenancePlanStatus Status { get; set; }
    }
}
