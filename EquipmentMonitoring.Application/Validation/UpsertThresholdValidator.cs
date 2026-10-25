using EquipmentMonitoring.Application.DTOs.Threshold;

namespace EquipmentMonitoring.Application.Validation
{
    public class UpsertThresholdValidator
    {
        public EquipmentValidationResult Validate(UpsertThresholdDto dto)
        {
            var result = new EquipmentValidationResult();

            if (dto.MinValue is null && dto.MaxValue is null)
            {
                const string message = "Вкажіть хоча б одну межу: мінімальне або максимальне значення.";
                result.AddError(nameof(UpsertThresholdDto.MinValue), message);
                result.AddError(nameof(UpsertThresholdDto.MaxValue), message);
                return result;
            }

            if (dto.MinValue is not null && !double.IsFinite(dto.MinValue.Value))
            {
                result.AddError(nameof(UpsertThresholdDto.MinValue), "Мінімальне значення має бути скінченним числом.");
            }

            if (dto.MaxValue is not null && !double.IsFinite(dto.MaxValue.Value))
            {
                result.AddError(nameof(UpsertThresholdDto.MaxValue), "Максимальне значення має бути скінченним числом.");
            }

            if (result.IsValid && dto.MinValue > dto.MaxValue)
            {
                result.AddError(nameof(UpsertThresholdDto.MinValue), "Мінімальне значення не може перевищувати максимальне.");
                result.AddError(nameof(UpsertThresholdDto.MaxValue), "Максимальне значення не може бути меншим за мінімальне.");
            }

            return result;
        }
    }
}
