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
    public class EquipmentThresholdsController : ThresholdsControllerBase
    {
        private readonly ThresholdService _thresholdService;

        public EquipmentThresholdsController(ThresholdService thresholdService, UpsertThresholdValidator validator)
            : base(thresholdService, validator)
        {
            _thresholdService = thresholdService;
        }

        [HttpGet]
        [ProducesResponseType<IReadOnlyList<ThresholdDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status404NotFound)]
        public Task<ActionResult<IReadOnlyList<ThresholdDto>>> ListAsync(Guid equipmentId, CancellationToken cancellationToken)
        {
            return ListCoreAsync(equipmentId, cancellationToken);
        }

        [HttpGet("effective")]
        [ProducesResponseType<IReadOnlyList<ThresholdDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyList<ThresholdDto>>> GetEffectiveAsync(Guid equipmentId, CancellationToken cancellationToken)
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
        [ProducesResponseType<ThresholdDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status404NotFound)]
        public Task<ActionResult<ThresholdDto>> GetAsync(Guid equipmentId, string measurementType, CancellationToken cancellationToken)
        {
            return GetCoreAsync(equipmentId, measurementType, cancellationToken);
        }

        [HttpPut("{measurementType}")]
        [Consumes("application/json")]
        [ProducesResponseType<ThresholdDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ThresholdDto>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict)]
        public Task<ActionResult<ThresholdDto>> UpsertAsync(
            Guid equipmentId,
            string measurementType,
            [FromBody] UpsertThresholdDto dto,
            CancellationToken cancellationToken)
        {
            return UpsertCoreAsync(equipmentId, measurementType, dto, cancellationToken);
        }

        [HttpDelete("{measurementType}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status404NotFound)]
        public Task<IActionResult> DeleteAsync(Guid equipmentId, string measurementType, CancellationToken cancellationToken)
        {
            return DeleteCoreAsync(equipmentId, measurementType, cancellationToken);
        }

        protected override string GetLocation(Guid? equipmentId, MeasurementType measurementType)
        {
            return $"/api/equipment/{equipmentId}/thresholds/{measurementType}";
        }
    }
}
