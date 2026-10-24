using EquipmentMonitoring.Application.DTOs.Measurement;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Application.Validation
{
    public class CreateMeasurementValidator
    {
        public EquipmentValidationResult Validate(CreateMeasurementDto dto)
        {
            var result = new EquipmentValidationResult();

            if (dto.Type is null)
            {
                AddError(result, nameof(CreateMeasurementDto.Type), "Вкажіть тип показника.");
            }
            else if (!Enum.IsDefined(typeof(MeasurementType), dto.Type.Value))
            {
                AddError(result, nameof(CreateMeasurementDto.Type), "Неприпустимий тип показника.");
            }

            if (dto.Value is null)
            {
                AddError(result, nameof(CreateMeasurementDto.Value), "Вкажіть числове значення показника.");
            }
            else if (!double.IsFinite(dto.Value.Value))
            {
                AddError(result, nameof(CreateMeasurementDto.Value), "Значення показника має бути скінченним числом.");
            }

            if (string.IsNullOrWhiteSpace(dto.Unit))
            {
                AddError(result, nameof(CreateMeasurementDto.Unit), "Вкажіть одиницю вимірювання.");
            }
            else if (dto.Type is not null && Enum.IsDefined(typeof(MeasurementType), dto.Type.Value))
            {
                var expectedUnit = GetExpectedUnit(dto.Type.Value);

                if (!string.Equals(dto.Unit, expectedUnit, StringComparison.Ordinal))
                {
                    AddError(result, nameof(CreateMeasurementDto.Unit), $"Для типу {dto.Type.Value} допустима одиниця: {expectedUnit}.");
                }
            }

            if (dto.MeasuredAt is null)
            {
                AddError(result, nameof(CreateMeasurementDto.MeasuredAt), "Вкажіть дату й час вимірювання.");
            }

            return result;
        }

        private static string GetExpectedUnit(MeasurementType type)
        {
            return type switch
            {
                MeasurementType.Temperature => "°C",
                MeasurementType.Voltage => "V",
                MeasurementType.Load => "%",
                _ => string.Empty
            };
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
