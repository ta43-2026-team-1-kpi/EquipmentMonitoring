using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.DTOs.Maintenance;

namespace EquipmentMonitoring.Application.Validation
{
    public class MaintenancePlanRegistrationValidator
    {
        private readonly CreateMaintenancePlanValidator _createMaintenancePlanValidator;
        private readonly IEquipmentRepository _equipmentRepository;

        public MaintenancePlanRegistrationValidator(
            CreateMaintenancePlanValidator createMaintenancePlanValidator,
            IEquipmentRepository equipmentRepository)
        {
            _createMaintenancePlanValidator = createMaintenancePlanValidator;
            _equipmentRepository = equipmentRepository;
        }

        public async Task<EquipmentValidationResult> ValidateAsync(
            CreateMaintenancePlanDto dto,
            CancellationToken cancellationToken = default)
        {
            var result = _createMaintenancePlanValidator.Validate(dto);
            if (!result.IsValid || dto.EquipmentId is null)
            {
                return result;
            }

            var equipmentExists = await _equipmentRepository.ExistsByIdAsync(
                dto.EquipmentId.Value,
                cancellationToken);

            if (!equipmentExists)
            {
                result.Errors[nameof(dto.EquipmentId)] =
                [
                    "Обладнання з таким ідентифікатором не знайдено."
                ];
            }

            return result;
        }
    }
}
