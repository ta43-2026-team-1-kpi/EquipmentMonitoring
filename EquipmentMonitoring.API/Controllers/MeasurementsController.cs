using System.Globalization;
using System.Text.RegularExpressions;
using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.DTOs.Measurement;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Application.Validation;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentMonitoring.API.Controllers
{
    [ApiController]
    [Route("api/equipment/{equipmentId:guid}/measurements")]
    public class MeasurementsController : ControllerBase
    {
        private static readonly Regex OffsetPattern = new(
            "(Z|[+-]\\d{2}:\\d{2})$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly IEquipmentRepository _equipmentRepository;
        private readonly IMeasurementRepository _measurementRepository;
        private readonly MeasurementRegistrationValidator _measurementRegistrationValidator;

        public MeasurementsController(
            IEquipmentRepository equipmentRepository,
            IMeasurementRepository measurementRepository,
            MeasurementRegistrationValidator measurementRegistrationValidator)
        {
            _equipmentRepository = equipmentRepository;
            _measurementRepository = measurementRepository;
            _measurementRegistrationValidator = measurementRegistrationValidator;
        }

        [HttpPost]
        public async Task<ActionResult<MeasurementDto>> CreateAsync(
            Guid equipmentId,
            [FromBody] CreateMeasurementDto dto,
            CancellationToken cancellationToken)
        {
            var validationResult = await _measurementRegistrationValidator.ValidateAsync(
                equipmentId,
                dto,
                cancellationToken);

            if (!validationResult.IsValid)
            {
                if (validationResult.Errors.ContainsKey("equipmentId"))
                {
                    return NotFound(CreateValidationProblemDetails(
                        validationResult.Errors.ToDictionary(
                            pair => pair.Key,
                            pair => pair.Value.ToArray()),
                        StatusCodes.Status404NotFound,
                        "Обладнання не знайдено."));
                }

                return BadRequest(CreateValidationProblemDetails(
                    validationResult.Errors.ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value.ToArray()),
                    StatusCodes.Status400BadRequest,
                    "Помилка валідації запиту."));
            }

            var measuredAtUtc = dto.MeasuredAt!.Value.UtcDateTime;
            var measurement = new Measurement(
                equipmentId,
                dto.Type!.Value,
                dto.Value!.Value,
                dto.Unit!,
                measuredAtUtc);

            await _measurementRepository.AddAsync(measurement, cancellationToken);

            try
            {
                await _measurementRepository.SaveChangesAsync(cancellationToken);
            }
            catch (MeasurementEquipmentNotFoundException)
            {
                return NotFound(CreateValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["equipmentId"] = ["Обладнання з таким ідентифікатором не знайдено."]
                    },
                    StatusCodes.Status404NotFound,
                    "Обладнання не знайдено."));
            }

            return StatusCode(StatusCodes.Status201Created, MapToDto(measurement));
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<MeasurementDto>>> GetHistoryAsync(
            Guid equipmentId,
            [FromQuery] string? type,
            [FromQuery] string? from,
            [FromQuery] string? to,
            CancellationToken cancellationToken)
        {
            if (!TryParseType(type, out var parsedType))
            {
                return BadRequest(CreateValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["type"] =
                        [
                            "Вкажіть один із допустимих типів: Temperature, Voltage, Load."
                        ]
                    },
                    StatusCodes.Status400BadRequest,
                    "Помилка валідації запиту."));
            }

            if (!TryParseRangeDate(from, "from", out var fromUtc, out var fromError))
            {
                return BadRequest(CreateValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["from"] = [fromError]
                    },
                    StatusCodes.Status400BadRequest,
                    "Помилка валідації запиту."));
            }

            if (!TryParseRangeDate(to, "to", out var toUtc, out var toError))
            {
                return BadRequest(CreateValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["to"] = [toError]
                    },
                    StatusCodes.Status400BadRequest,
                    "Помилка валідації запиту."));
            }

            if (fromUtc is not null && toUtc is not null && fromUtc.Value > toUtc.Value)
            {
                return BadRequest(CreateValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["from"] = ["Параметр from не може бути пізнішим за to."],
                        ["to"] = ["Параметр to не може бути ранішим за from."]
                    },
                    StatusCodes.Status400BadRequest,
                    "Помилка валідації запиту."));
            }

            var equipmentExists = await _equipmentRepository.ExistsByIdAsync(
                equipmentId,
                cancellationToken);

            if (!equipmentExists)
            {
                return NotFound(CreateValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["equipmentId"] = ["Обладнання з таким ідентифікатором не знайдено."]
                    },
                    StatusCodes.Status404NotFound,
                    "Обладнання не знайдено."));
            }

            var history = await _measurementRepository.GetByEquipmentAsync(
                equipmentId,
                parsedType,
                fromUtc,
                toUtc,
                cancellationToken);

            return Ok(history.Select(MapToDto).ToList());
        }

        private bool TryParseType(string? type, out MeasurementType? parsedType)
        {
            parsedType = null;

            if (type is null)
            {
                return true;
            }

            if (Request.Query.ContainsKey("type") && string.IsNullOrWhiteSpace(type))
            {
                return false;
            }

            foreach (var allowedType in Enum.GetValues<MeasurementType>())
            {
                if (string.Equals(type, allowedType.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    parsedType = allowedType;
                    return true;
                }
            }

            return false;
        }

        private bool TryParseRangeDate(string? value, string queryParameterName, out DateTime? utcDate, out string error)
        {
            utcDate = null;
            error = string.Empty;

            if (value is null)
            {
                if (Request.Query.ContainsKey(queryParameterName))
                {
                    error = "Параметр часу не може бути порожнім.";
                    return false;
                }

                return true;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                error = "Параметр часу не може бути порожнім.";
                return false;
            }

            if (!OffsetPattern.IsMatch(value))
            {
                error = "Дата й час мають містити часовий зсув або суфікс Z.";
                return false;
            }

            if (!DateTimeOffset.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed))
            {
                error = "Вкажіть дату й час у форматі ISO 8601.";
                return false;
            }

            utcDate = parsed.UtcDateTime;
            return true;
        }

        private static MeasurementDto MapToDto(Measurement measurement)
        {
            return new MeasurementDto
            {
                Id = measurement.Id,
                EquipmentId = measurement.EquipmentId,
                Type = measurement.Type,
                Value = measurement.Value,
                Unit = measurement.Unit,
                MeasuredAtUtc = new DateTimeOffset(
                    DateTime.SpecifyKind(measurement.MeasuredAtUtc, DateTimeKind.Utc),
                    TimeSpan.Zero)
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
    }
}
