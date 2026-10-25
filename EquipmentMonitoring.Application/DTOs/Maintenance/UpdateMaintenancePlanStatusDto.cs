using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.DTOs.Maintenance
{
    public class UpdateMaintenancePlanStatusDto
    {
        public MaintenancePlanStatus? Status { get; set; }
    }
}
