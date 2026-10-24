using EquipmentMonitoring.Application.DTOs.Equipment;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.Validation
{
    public class CreateEquipmentValidator
    {
        public EquipmentValidationResult Validate(CreateEquipmentDto dto)
        {
            var result = new EquipmentValidationResult();

            ValidateRequiredAndLength(dto.Name, nameof(CreateEquipmentDto.Name), "Назва", 200, "Вкажіть назву обладнання.", result);
            ValidateRequiredAndLength(dto.Type, nameof(CreateEquipmentDto.Type), "Тип", 100, "Вкажіть тип обладнання.", result);
            ValidateRequiredAndLength(dto.SerialNumber, nameof(CreateEquipmentDto.SerialNumber), "Серійний номер", 100, "Вкажіть серійний номер обладнання.", result);
            ValidateRequiredAndLength(dto.Location, nameof(CreateEquipmentDto.Location), "Місце встановлення", 200, "Вкажіть місце встановлення обладнання.", result);

            if (dto.Status == null)
            {
                AddError(result, nameof(CreateEquipmentDto.Status), "Вкажіть стан обладнання.");
            }
            else if (!Enum.IsDefined(typeof(EquipmentStatus), dto.Status.Value))
            {
                AddError(result, nameof(CreateEquipmentDto.Status), "Неприпустимий стан обладнання.");
            }

            return result;
        }

        private static void ValidateRequiredAndLength(
            string? value,
            string fieldName,
            string fieldDisplayName,
            int maxLength,
            string requiredMessage,
            EquipmentValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                AddError(result, fieldName, requiredMessage);
                return;
            }

            if (value.Length > maxLength)
            {
                AddError(result, fieldName, $"Поле «{fieldDisplayName}» не може перевищувати {maxLength} символів.");
            }
        }

        private static void AddError(EquipmentValidationResult result, string fieldName, string message)
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
