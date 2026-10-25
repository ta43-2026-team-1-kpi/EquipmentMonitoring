using EquipmentMonitoring.Application.DTOs.Threshold;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Application.Services;
using EquipmentMonitoring.Application.Validation;
using EquipmentMonitoring.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentMonitoring.API.Controllers.Monitoring
{
    /// <summary>
    /// Shared endpoint logic for global (<c>equipmentId == null</c>) and per-equipment thresholds.
    /// </summary>
    public abstract class ThresholdsControllerBase : MonitoringControllerBase
    {
        private readonly ThresholdService _thresholdService;
        private readonly UpsertThresholdValidator _validator;

        protected ThresholdsControllerBase(ThresholdService thresholdService, UpsertThresholdValidator validator)
        {
            _thresholdService = thresholdService;
            _validator = validator;
        }

        protected abstract string GetLocation(Guid? equipmentId, MeasurementType measurementType);

        protected async Task<ActionResult<IReadOnlyList<ThresholdDto>>> ListCoreAsync(
            Guid? equipmentId,
            CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _thresholdService.ListAsync(equipmentId, cancellationToken));
            }
            catch (EquipmentNotFoundException)
            {
                return EquipmentNotFound();
            }
        }

        protected async Task<ActionResult<ThresholdDto>> GetCoreAsync(
            Guid? equipmentId,
            string measurementType,
            CancellationToken cancellationToken)
        {
            if (!TryParseMeasurementType(measurementType, out var type, out var error))
            {
                return error;
            }

            try
            {
                var threshold = await _thresholdService.GetAsync(equipmentId, type, cancellationToken);
                return threshold is null ? ThresholdNotFound(type) : Ok(threshold);
            }
            catch (EquipmentNotFoundException)
            {
                return EquipmentNotFound();
            }
        }

        protected async Task<ActionResult<ThresholdDto>> UpsertCoreAsync(
            Guid? equipmentId,
            string measurementType,
            UpsertThresholdDto dto,
            CancellationToken cancellationToken)
        {
            if (!TryParseMeasurementType(measurementType, out var type, out var error))
            {
                return error;
            }

            var validationResult = _validator.Validate(dto);

            if (!validationResult.IsValid)
            {
                return ValidationFailed(validationResult.Errors);
            }

            try
            {
                var result = await _thresholdService.UpsertAsync(equipmentId, type, dto, cancellationToken);

                return result.Created
                    ? Created(GetLocation(equipmentId, type), result.Threshold)
                    : Ok(result.Threshold);
            }
            catch (EquipmentNotFoundException)
            {
                return EquipmentNotFound();
            }
            catch (ThresholdConflictException exception)
            {
                return ProblemResult(
                    StatusCodes.Status409Conflict,
                    "Конфлікт паралельного оновлення межі.",
                    new Dictionary<string, string[]> { ["measurementType"] = [exception.Message] });
            }
        }

        protected async Task<IActionResult> DeleteCoreAsync(
            Guid? equipmentId,
            string measurementType,
            CancellationToken cancellationToken)
        {
            if (!TryParseMeasurementType(measurementType, out var type, out var error))
            {
                return error;
            }

            try
            {
                return await _thresholdService.DeleteAsync(equipmentId, type, cancellationToken)
                    ? NoContent()
                    : ThresholdNotFound(type);
            }
            catch (EquipmentNotFoundException)
            {
                return EquipmentNotFound();
            }
        }

        private bool TryParseMeasurementType(string value, out MeasurementType type, out ObjectResult error)
        {
            error = null!;

            if (TryParseEnumName(value, out type))
            {
                return true;
            }

            error = ValidationFailed("measurementType", $"Вкажіть один із допустимих типів: {AllowedValues<MeasurementType>()}.");
            return false;
        }

        private ObjectResult ThresholdNotFound(MeasurementType type)
        {
            return ProblemResult(
                StatusCodes.Status404NotFound,
                "Межу не знайдено.",
                new Dictionary<string, string[]> { ["measurementType"] = [$"Межу для показника {type} не задано."] });
        }
    }
}
