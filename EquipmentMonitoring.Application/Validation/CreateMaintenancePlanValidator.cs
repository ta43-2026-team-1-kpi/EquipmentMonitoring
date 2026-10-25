using EquipmentMonitoring.Application.DTOs.Maintenance;

namespace EquipmentMonitoring.Application.Validation
{
    public class CreateMaintenancePlanValidator
    {
        public const int WorkTypeMaxLength = 200;
        public const int ResponsibleEmployeeMaxLength = 200;
        public const int NoteMaxLength = 1000;

        public EquipmentValidationResult Validate(CreateMaintenancePlanDto dto)
        {
            var result = new EquipmentValidationResult();

            if (dto.EquipmentId is null || dto.EquipmentId == Guid.Empty)
            {
                AddError(result, nameof(dto.EquipmentId), "Вкажіть зареєстроване обладнання.");
            }

            if (dto.PlannedDate is null || dto.PlannedDate == DateOnly.MinValue)
            {
                AddError(result, nameof(dto.PlannedDate), "Вкажіть заплановану дату обслуговування.");
            }
            else if (dto.PlannedDate.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                AddError(
                    result,
                    nameof(dto.PlannedDate),
                    "Запланована дата не може бути в минулому.");
            }

            if (string.IsNullOrWhiteSpace(dto.WorkType))
            {
                AddError(result, nameof(dto.WorkType), "Вкажіть вид робіт.");
            }
            else if (dto.WorkType.Length > WorkTypeMaxLength)
            {
                AddError(result, nameof(dto.WorkType), $"Вид робіт не може перевищувати {WorkTypeMaxLength} символів.");
            }

            if (string.IsNullOrWhiteSpace(dto.ResponsibleEmployee))
            {
                AddError(result, nameof(dto.ResponsibleEmployee), "Вкажіть відповідального працівника.");
            }
            else if (dto.ResponsibleEmployee.Length > ResponsibleEmployeeMaxLength)
            {
                AddError(result, nameof(dto.ResponsibleEmployee), $"Ім'я відповідального не може перевищувати {ResponsibleEmployeeMaxLength} символів.");
            }

            if (dto.Note?.Length > NoteMaxLength)
            {
                AddError(result, nameof(dto.Note), $"Примітка не може перевищувати {NoteMaxLength} символів.");
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
