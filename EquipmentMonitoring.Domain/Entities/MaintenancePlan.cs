using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Domain.Entities
{
    public class MaintenancePlan
    {
        public Guid Id { get; private set; }

        public Guid EquipmentId { get; private set; }

        public DateOnly PlannedDate { get; private set; }

        public string WorkType { get; private set; } = string.Empty;

        public string ResponsibleEmployee { get; private set; } = string.Empty;

        public string? Note { get; private set; }

        public MaintenancePlanStatus Status { get; private set; }

        public Equipment Equipment { get; private set; } = null!;

        public MaintenancePlan(
            Guid equipmentId,
            DateOnly plannedDate,
            string workType,
            string responsibleEmployee,
            string? note)
        {
            Id = Guid.NewGuid();
            EquipmentId = equipmentId;
            PlannedDate = plannedDate;
            WorkType = workType;
            ResponsibleEmployee = responsibleEmployee;
            Note = note;
            Status = MaintenancePlanStatus.Planned;
        }

        public bool TryChangeStatus(MaintenancePlanStatus status)
        {
            if (Status != MaintenancePlanStatus.Planned
                || status is not MaintenancePlanStatus.Completed
                    and not MaintenancePlanStatus.Cancelled)
            {
                return false;
            }

            Status = status;
            return true;
        }
    }
}
