using System.ComponentModel;
using EquipmentMonitoring.Application.DTOs.Threshold;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Application.Services;
using EquipmentMonitoring.Application.Validation;
using EquipmentMonitoring.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentMonitoring.API.Controllers.Monitoring
{
    [ApiController]
    [Route("api/equipment/{equipmentId:guid}/thresholds")]
    [Tags("Thresholds")]
    public class EquipmentThresholdsController : ThresholdsControllerBase
    {
        private const string EquipmentIdDescription = "Ідентифікатор обладнання.";
        private const string MeasurementTypeDescription = "Тип показника: Temperature, Voltage, Load (без урахування регістру).";

        private readonly ThresholdService _thresholdService;

        public EquipmentThresholdsController(ThresholdService thresholdService, UpsertThresholdValidator validator)
            : base(thresholdService, validator)
        {
            _thresholdService = thresholdService;
        }

        [HttpGet]
        [EndpointSummary("Власні межі обладнання")]
        [EndpointDescription("Повертає межі, задані саме для цього обладнання. Глобальні межі не включаються.")]
        [ProducesResponseType<IReadOnlyList<ThresholdDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status404NotFound)]
        public Task<ActionResult<IReadOnlyList<ThresholdDto>>> ListAsync(
            [Description(EquipmentIdDescription)] Guid equipmentId,
            CancellationToken cancellationToken)
        {
            return ListCoreAsync(equipmentId, cancellationToken);
        }

        [HttpGet("effective")]
        [EndpointSummary("Чинні межі обладнання")]
        [EndpointDescription("Повертає межі, за якими зараз перевіряються вимірювання обладнання: власна межа, якщо задана, інакше глобальна. Типи без жодної межі не повертаються.")]
        [ProducesResponseType<IReadOnlyList<ThresholdDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyList<ThresholdDto>>> GetEffectiveAsync(
            [Description(EquipmentIdDescription)] Guid equipmentId,
            CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _thresholdService.GetEffectiveAsync(equipmentId, cancellationToken));
            }
            catch (EquipmentNotFoundException)
            {
                return EquipmentNotFound();
            }
        }

        [HttpGet("{measurementType}")]
        [EndpointSummary("Власна межа обладнання для типу показника")]
        [EndpointDescription("Повертає межу, задану саме для цього обладнання. Глобальна межа тут не враховується.")]
        [ProducesResponseType<ThresholdDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status404NotFound)]
        public Task<ActionResult<ThresholdDto>> GetAsync(
            [Description(EquipmentIdDescription)] Guid equipmentId,
            [Description(MeasurementTypeDescription)] string measurementType,
            CancellationToken cancellationToken)
        {
            return GetCoreAsync(equipmentId, measurementType, cancellationToken);
        }

        [HttpPut("{measurementType}")]
        [Consumes("application/json")]
        [EndpointSummary("Встановлення межі для обладнання")]
        [EndpointDescription("Створює (201) або замінює (200) межу для обладнання. Вона має пріоритет над глобальною межею того самого типу, навіть якщо м'якша.")]
        [ProducesResponseType<ThresholdDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ThresholdDto>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict)]
        public Task<ActionResult<ThresholdDto>> UpsertAsync(
            [Description(EquipmentIdDescription)] Guid equipmentId,
            [Description(MeasurementTypeDescription)] string measurementType,
            [FromBody] UpsertThresholdDto dto,
            CancellationToken cancellationToken)
        {
            return UpsertCoreAsync(equipmentId, measurementType, dto, cancellationToken);
        }

        [HttpDelete("{measurementType}")]
        [EndpointSummary("Видалення межі обладнання")]
        [EndpointDescription("Видаляє власну межу обладнання; після цього діє глобальна межа, якщо вона задана.")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status404NotFound)]
        public Task<IActionResult> DeleteAsync(
            [Description(EquipmentIdDescription)] Guid equipmentId,
            [Description(MeasurementTypeDescription)] string measurementType,
            CancellationToken cancellationToken)
        {
            return DeleteCoreAsync(equipmentId, measurementType, cancellationToken);
        }

        protected override string GetLocation(Guid? equipmentId, MeasurementType measurementType)
        {
            return $"/api/equipment/{equipmentId}/thresholds/{measurementType}";
        }
    }
}
