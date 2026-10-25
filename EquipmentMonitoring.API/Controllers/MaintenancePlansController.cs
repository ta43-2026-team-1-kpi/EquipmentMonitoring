using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.DTOs.Maintenance;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Application.Validation;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentMonitoring.API.Controllers
{
    [ApiController]
    [Route("api/maintenance-plans")]
    public class MaintenancePlansController : ControllerBase
    {
        private readonly IEquipmentRepository _equipmentRepository;
        private readonly IMaintenancePlanRepository _maintenancePlanRepository;
        private readonly MaintenancePlanRegistrationValidator _registrationValidator;
        private readonly UpdateMaintenancePlanStatusValidator _statusValidator;

        public MaintenancePlansController(
            IEquipmentRepository equipmentRepository,
            IMaintenancePlanRepository maintenancePlanRepository,
            MaintenancePlanRegistrationValidator registrationValidator,
            UpdateMaintenancePlanStatusValidator statusValidator)
        {
            _equipmentRepository = equipmentRepository;
            _maintenancePlanRepository = maintenancePlanRepository;
            _registrationValidator = registrationValidator;
            _statusValidator = statusValidator;
        }

        [HttpPost]
        public async Task<ActionResult<MaintenancePlanDto>> CreateAsync(
            [FromBody] CreateMaintenancePlanDto dto,
            CancellationToken cancellationToken)
        {
            var validationResult = await _registrationValidator.ValidateAsync(
                dto,
                cancellationToken);

            if (!validationResult.IsValid)
            {
                if (dto.EquipmentId is not null
                    && dto.EquipmentId != Guid.Empty
                    && validationResult.Errors.ContainsKey(
                        nameof(CreateMaintenancePlanDto.EquipmentId)))
                {
                    return NotFound(CreateValidationProblemDetails(
                        ToErrorDictionary(validationResult.Errors),
                        StatusCodes.Status404NotFound,
                        "Обладнання не знайдено."));
                }

                return BadRequest(CreateValidationProblemDetails(
                    ToErrorDictionary(validationResult.Errors),
                    StatusCodes.Status400BadRequest,
                    "Помилка валідації запиту."));
            }

            var maintenancePlan = new MaintenancePlan(
                dto.EquipmentId!.Value,
                dto.PlannedDate!.Value,
                dto.WorkType!,
                dto.ResponsibleEmployee!,
                dto.Note);

            await _maintenancePlanRepository.AddAsync(
                maintenancePlan,
                cancellationToken);

            try
            {
                await _maintenancePlanRepository.SaveChangesAsync(
                    cancellationToken);
            }
            catch (MaintenancePlanEquipmentNotFoundException)
            {
                return NotFound(CreateValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        [nameof(CreateMaintenancePlanDto.EquipmentId)] =
                        [
                            "Обладнання з таким ідентифікатором не знайдено."
                        ]
                    },
                    StatusCodes.Status404NotFound,
                    "Обладнання не знайдено."));
            }

            return StatusCode(
                StatusCodes.Status201Created,
                MapToDto(maintenancePlan));
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<MaintenancePlanDto>>> GetAllAsync(
            [FromQuery] Guid? equipmentId,
            CancellationToken cancellationToken)
        {
            if (equipmentId == Guid.Empty)
            {
                return BadRequest(CreateValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["equipmentId"] = ["Вкажіть коректний ідентифікатор обладнання."]
                    },
                    StatusCodes.Status400BadRequest,
                    "Помилка валідації запиту."));
            }

            if (equipmentId is not null
                && !await _equipmentRepository.ExistsByIdAsync(
                    equipmentId.Value,
                    cancellationToken))
            {
                return NotFound(CreateValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["equipmentId"] = ["Обладнання з таким ідентифікатором не знайдено."]
                    },
                    StatusCodes.Status404NotFound,
                    "Обладнання не знайдено."));
            }

            var plans = await _maintenancePlanRepository.GetAllAsync(
                equipmentId,
                cancellationToken);

            return Ok(plans.Select(MapToDto).ToList());
        }

        [HttpPatch("{maintenancePlanId:guid}/status")]
        public async Task<ActionResult<MaintenancePlanDto>> UpdateStatusAsync(
            Guid maintenancePlanId,
            [FromBody] UpdateMaintenancePlanStatusDto dto,
            CancellationToken cancellationToken)
        {
            var validationResult = _statusValidator.Validate(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(CreateValidationProblemDetails(
                    ToErrorDictionary(validationResult.Errors),
                    StatusCodes.Status400BadRequest,
                    "Помилка валідації запиту."));
            }

            var maintenancePlan = await _maintenancePlanRepository.GetByIdAsync(
                maintenancePlanId,
                cancellationToken);

            if (maintenancePlan is null)
            {
                return NotFound(CreateValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["maintenancePlanId"] = ["План технічного обслуговування не знайдено."]
                    },
                    StatusCodes.Status404NotFound,
                    "План технічного обслуговування не знайдено."));
            }

            if (!maintenancePlan.TryChangeStatus(dto.Status!.Value))
            {
                return Conflict(CreateValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        [nameof(dto.Status)] =
                        [
                            "Змінити стан можна лише для плану зі станом Planned."
                        ]
                    },
                    StatusCodes.Status409Conflict,
                    "Стан плану вже змінено."));
            }

            await _maintenancePlanRepository.SaveChangesAsync(cancellationToken);

            return Ok(MapToDto(maintenancePlan));
        }

        private static MaintenancePlanDto MapToDto(MaintenancePlan maintenancePlan)
        {
            return new MaintenancePlanDto
            {
                Id = maintenancePlan.Id,
                EquipmentId = maintenancePlan.EquipmentId,
                PlannedDate = maintenancePlan.PlannedDate,
                WorkType = maintenancePlan.WorkType,
                ResponsibleEmployee = maintenancePlan.ResponsibleEmployee,
                Note = maintenancePlan.Note,
                Status = maintenancePlan.Status
            };
        }

        private static ValidationProblemDetails CreateValidationProblemDetails(
            Dictionary<string, string[]> errors,
            int status,
            string title)
        {
            return new ValidationProblemDetails(errors)
            {
                Status = status,
                Title = title
            };
        }

        private static Dictionary<string, string[]> ToErrorDictionary(
            Dictionary<string, List<string>> errors)
        {
            return errors.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ToArray());
        }
    }
}
