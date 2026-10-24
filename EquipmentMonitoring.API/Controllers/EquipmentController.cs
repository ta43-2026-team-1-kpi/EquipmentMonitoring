using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.DTOs.Equipment;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Application.Validation;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentMonitoring.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EquipmentController : ControllerBase
    {
        private readonly IEquipmentRepository _equipmentRepository;
        private readonly EquipmentRegistrationValidator _equipmentRegistrationValidator;

        public EquipmentController(
            IEquipmentRepository equipmentRepository,
            EquipmentRegistrationValidator equipmentRegistrationValidator)
        {
            _equipmentRepository = equipmentRepository;
            _equipmentRegistrationValidator = equipmentRegistrationValidator;
        }

        [HttpPost]
        public async Task<ActionResult<EquipmentDto>> CreateAsync(
            [FromBody] CreateEquipmentDto dto,
            CancellationToken cancellationToken)
        {
            var validationResult =
                await _equipmentRegistrationValidator.ValidateAsync(
                    dto, cancellationToken);

            if (!validationResult.IsValid)
            {
                return BadRequest(CreateValidationProblemDetails(
                    validationResult.Errors.ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value.ToArray())));
            }

            var equipment = new Equipment(
                dto.Name!,
                dto.Type!,
                dto.SerialNumber!,
                dto.Location!,
                dto.Status!.Value);

            await _equipmentRepository.AddAsync(
                equipment, cancellationToken);

            try
            {
                await _equipmentRepository.SaveChangesAsync(
                    cancellationToken);
            }
            catch (DuplicateEquipmentSerialNumberException)
            {
                var conflict = new ValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        [nameof(CreateEquipmentDto.SerialNumber)] =
                        [
                            "Обладнання з таким серійним номером уже існує."
                        ]
                    })
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Конфлікт унікальності серійного номера."
                };

                return Conflict(conflict);
            }

            return StatusCode(
                StatusCodes.Status201Created,
                MapToDto(equipment));
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<EquipmentDto>>> GetAllAsync(
            [FromQuery] string? status,
            CancellationToken cancellationToken)
        {
            EquipmentStatus? parsedStatus = null;

            // Порожній query-параметр може бути прив'язаний як null.
            // Тому окремо перевіряємо його наявність у запиті.
            if (status is not null || Request.Query.ContainsKey("status"))
            {
                if (!TryParseStatus(status, out var statusValue))
                {
                    return BadRequest(CreateValidationProblemDetails(
                        new Dictionary<string, string[]>
                        {
                            ["status"] =
                            [
                                "Вкажіть один із допустимих станів: Working, Faulty, UnderMaintenance."
                            ]
                        }));
                }

                parsedStatus = statusValue;
            }

            var equipment = await _equipmentRepository.GetAllAsync(
                parsedStatus, cancellationToken);

            var response = equipment.Select(MapToDto).ToList();

            return Ok(response);
        }

        private static bool TryParseStatus(
            string? status,
            out EquipmentStatus parsedStatus)
        {
            parsedStatus = default;

            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            foreach (var allowedStatus in Enum.GetValues<EquipmentStatus>())
            {
                if (string.Equals(
                    status,
                    allowedStatus.ToString(),
                    StringComparison.OrdinalIgnoreCase))
                {
                    parsedStatus = allowedStatus;
                    return true;
                }
            }

            return false;
        }

        private static EquipmentDto MapToDto(Equipment equipment)
        {
            return new EquipmentDto
            {
                Id = equipment.Id,
                Name = equipment.Name,
                Type = equipment.Type,
                SerialNumber = equipment.SerialNumber,
                Location = equipment.Location,
                Status = equipment.Status
            };
        }

        private static ValidationProblemDetails CreateValidationProblemDetails(
            Dictionary<string, string[]> errors)
        {
            return new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Помилка валідації запиту."
            };
        }
    }
}
