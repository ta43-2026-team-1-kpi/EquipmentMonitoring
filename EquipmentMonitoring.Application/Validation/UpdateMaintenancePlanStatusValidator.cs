using EquipmentMonitoring.Application.DTOs.Maintenance;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.Validation
{
    public class UpdateMaintenancePlanStatusValidator
    {
        public EquipmentValidationResult Validate(UpdateMaintenancePlanStatusDto dto)
        {
            var result = new EquipmentValidationResult();

            if (dto.Status is null)
            {
                AddError(result, nameof(dto.Status), "Вкажіть новий стан обслуговування.");
            }
            else if (!Enum.IsDefined(typeof(MaintenancePlanStatus), dto.Status.Value))
            {
                AddError(result, nameof(dto.Status), "Неприпустимий стан обслуговування.");
            }
            else if (dto.Status is not MaintenancePlanStatus.Completed
                and not MaintenancePlanStatus.Cancelled)
            {
                AddError(result, nameof(dto.Status), "Стан можна змінити лише на Completed або Cancelled.");
            }

            return result;
        }

        private static void AddError(
            EquipmentValidationResult result,
            string fieldName,
            string message)
        {
            if (!result.Errors.TryGetValue(fieldName, out var messages))
            {
                messages = [];
                result.Errors[fieldName] = messages;
            }

            messages.Add(message);
        }
    }
}
