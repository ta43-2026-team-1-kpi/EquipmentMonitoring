using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.DTOs.Equipment;

namespace EquipmentMonitoring.Application.Validation
{
    public class EquipmentRegistrationValidator
    {
        private readonly CreateEquipmentValidator _createEquipmentValidator;
        private readonly IEquipmentRepository _equipmentRepository;

        public EquipmentRegistrationValidator(
            CreateEquipmentValidator createEquipmentValidator,
            IEquipmentRepository equipmentRepository)
        {
            _createEquipmentValidator = createEquipmentValidator;
            _equipmentRepository = equipmentRepository;
        }

        public async Task<EquipmentValidationResult> ValidateAsync(
            CreateEquipmentDto dto,
            CancellationToken cancellationToken = default)
        {
            dto.SerialNumber = dto.SerialNumber?.Trim();

            var result = _createEquipmentValidator.Validate(dto);

            if (!result.IsValid)
            {
                return result;
            }

            var serialNumberExists = await _equipmentRepository.ExistsBySerialNumberAsync(
                dto.SerialNumber!,
                cancellationToken);

            if (serialNumberExists)
            {
                result.Errors[nameof(CreateEquipmentDto.SerialNumber)] =
                ["Обладнання з таким серійним номером уже існує."];
            }

            return result;
        }
    }
}
