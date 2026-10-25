using System.ComponentModel;
using EquipmentMonitoring.Application.DTOs.Threshold;
using EquipmentMonitoring.Application.Services;
using EquipmentMonitoring.Application.Validation;
using EquipmentMonitoring.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentMonitoring.API.Controllers.Monitoring
{
    [ApiController]
    [Route("api/thresholds")]
    [Tags("Thresholds")]
    public class ThresholdsController : ThresholdsControllerBase
    {
        private const string MeasurementTypeDescription = "Тип показника: Temperature, Voltage, Load (без урахування регістру).";

        public ThresholdsController(ThresholdService thresholdService, UpsertThresholdValidator validator)
            : base(thresholdService, validator)
        {
        }

        [HttpGet]
        [EndpointSummary("Глобальні межі")]
        [EndpointDescription("Повертає межі, що діють для всього обладнання, яке не має власної межі для цього типу показника.")]
        [ProducesResponseType<IReadOnlyList<ThresholdDto>>(StatusCodes.Status200OK)]
        public Task<ActionResult<IReadOnlyList<ThresholdDto>>> ListAsync(CancellationToken cancellationToken)
        {
            return ListCoreAsync(null, cancellationToken);
        }

        [HttpGet("{measurementType}")]
        [EndpointSummary("Глобальна межа для типу показника")]
        [EndpointDescription("Повертає глобальну межу для вказаного типу показника.")]
        [ProducesResponseType<ThresholdDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status404NotFound)]
        public Task<ActionResult<ThresholdDto>> GetAsync(
            [Description(MeasurementTypeDescription)] string measurementType,
            CancellationToken cancellationToken)
        {
            return GetCoreAsync(null, measurementType, cancellationToken);
        }

        [HttpPut("{measurementType}")]
        [Consumes("application/json")]
        [EndpointSummary("Встановлення глобальної межі")]
        [EndpointDescription("Створює (201) або замінює (200) глобальну межу для типу показника. Нові вимірювання перевіряються за оновленою межею; наявні попередження не змінюються.")]
        [ProducesResponseType<ThresholdDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ThresholdDto>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict)]
        public Task<ActionResult<ThresholdDto>> UpsertAsync(
            [Description(MeasurementTypeDescription)] string measurementType,
            [FromBody] UpsertThresholdDto dto,
            CancellationToken cancellationToken)
        {
            return UpsertCoreAsync(null, measurementType, dto, cancellationToken);
        }

        [HttpDelete("{measurementType}")]
        [EndpointSummary("Видалення глобальної межі")]
        [EndpointDescription("Видаляє глобальну межу. Створені раніше попередження зберігаються.")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status404NotFound)]
        public Task<IActionResult> DeleteAsync(
            [Description(MeasurementTypeDescription)] string measurementType,
            CancellationToken cancellationToken)
        {
            return DeleteCoreAsync(null, measurementType, cancellationToken);
        }

        protected override string GetLocation(Guid? equipmentId, MeasurementType measurementType)
        {
            return $"/api/thresholds/{measurementType}";
        }
    }
}
