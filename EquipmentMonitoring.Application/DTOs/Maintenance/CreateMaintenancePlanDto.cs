namespace EquipmentMonitoring.Application.DTOs.Maintenance
{
    public class CreateMaintenancePlanDto
    {
        public Guid? EquipmentId { get; set; }

        public DateOnly? PlannedDate { get; set; }

        public string? WorkType { get; set; }

        public string? ResponsibleEmployee { get; set; }

        public string? Note { get; set; }
    }
}
