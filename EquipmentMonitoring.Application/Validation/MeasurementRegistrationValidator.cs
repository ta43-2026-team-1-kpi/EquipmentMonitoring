using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.DTOs.Measurement;

namespace EquipmentMonitoring.Application.Validation
{
    public class MeasurementRegistrationValidator
    {
        private readonly CreateMeasurementValidator _createMeasurementValidator;
        private readonly IEquipmentRepository _equipmentRepository;

        public MeasurementRegistrationValidator(
            CreateMeasurementValidator createMeasurementValidator,
            IEquipmentRepository equipmentRepository)
        {
            _createMeasurementValidator = createMeasurementValidator;
            _equipmentRepository = equipmentRepository;
        }

        public async Task<EquipmentValidationResult> ValidateAsync(
            Guid equipmentId,
            CreateMeasurementDto dto,
            CancellationToken cancellationToken = default)
        {
            var result = _createMeasurementValidator.Validate(dto);

            if (!result.IsValid)
            {
                return result;
            }

            var equipmentExists = await _equipmentRepository.ExistsByIdAsync(
                equipmentId,
                cancellationToken);

            if (!equipmentExists)
            {
                result.Errors["equipmentId"] = ["Обладнання з таким ідентифікатором не знайдено."];
            }

            return result;
        }
    }
}
