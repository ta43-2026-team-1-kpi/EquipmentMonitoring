using EquipmentMonitoring.Application.DTOs.Threshold;
using EquipmentMonitoring.Application.Services;
using EquipmentMonitoring.Application.Validation;
using EquipmentMonitoring.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentMonitoring.API.Controllers.Monitoring
{
    [ApiController]
    [Route("api/thresholds")]
    public class ThresholdsController : ThresholdsControllerBase
    {
        public ThresholdsController(ThresholdService thresholdService, UpsertThresholdValidator validator)
            : base(thresholdService, validator)
        {
        }

        [HttpGet]
        [ProducesResponseType<IReadOnlyList<ThresholdDto>>(StatusCodes.Status200OK)]
        public Task<ActionResult<IReadOnlyList<ThresholdDto>>> ListAsync(CancellationToken cancellationToken)
        {
            return ListCoreAsync(null, cancellationToken);
        }

        [HttpGet("{measurementType}")]
        [ProducesResponseType<ThresholdDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status404NotFound)]
        public Task<ActionResult<ThresholdDto>> GetAsync(string measurementType, CancellationToken cancellationToken)
        {
            return GetCoreAsync(null, measurementType, cancellationToken);
        }

        [HttpPut("{measurementType}")]
        [Consumes("application/json")]
        [ProducesResponseType<ThresholdDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ThresholdDto>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict)]
        public Task<ActionResult<ThresholdDto>> UpsertAsync(
            string measurementType,
            [FromBody] UpsertThresholdDto dto,
            CancellationToken cancellationToken)
        {
            return UpsertCoreAsync(null, measurementType, dto, cancellationToken);
        }

        [HttpDelete("{measurementType}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status404NotFound)]
        public Task<IActionResult> DeleteAsync(string measurementType, CancellationToken cancellationToken)
        {
            return DeleteCoreAsync(null, measurementType, cancellationToken);
        }

        protected override string GetLocation(Guid? equipmentId, MeasurementType measurementType)
        {
            return $"/api/thresholds/{measurementType}";
        }
    }
}
