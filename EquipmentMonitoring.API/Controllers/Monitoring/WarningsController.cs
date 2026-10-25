using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.DTOs.Common;
using EquipmentMonitoring.Application.DTOs.Warning;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Application.Services;
using EquipmentMonitoring.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentMonitoring.API.Controllers.Monitoring
{
    [ApiController]
    [Route("api/warnings")]
    public class WarningsController : MonitoringControllerBase
    {
        private const string AllStatuses = "All";

        private readonly WarningService _warningService;

        public WarningsController(WarningService warningService)
        {
            _warningService = warningService;
        }

        /// <param name="status">Active (default), Acknowledged or All.</param>
        [HttpGet]
        [ProducesResponseType<PagedResultDto<WarningDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PagedResultDto<WarningDto>>> SearchAsync(
            [FromQuery] Guid? equipmentId,
            [FromQuery] string? status,
            [FromQuery] string? measurementType,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken)
        {
            var errors = new Dictionary<string, List<string>>();

            if (IsPresentButEmpty("equipmentId", equipmentId))
            {
                errors["equipmentId"] = ["Параметр equipmentId не може бути порожнім."];
            }

            WarningStatus? parsedStatus = WarningStatus.Active;

            if (status is not null || Request.Query.ContainsKey("status"))
            {
                if (string.Equals(status, AllStatuses, StringComparison.OrdinalIgnoreCase))
                {
                    parsedStatus = null;
                }
                else if (TryParseEnumName<WarningStatus>(status, out var statusValue))
                {
                    parsedStatus = statusValue;
                }
                else
                {
                    errors["status"] = [$"Вкажіть один із допустимих станів: {AllowedValues<WarningStatus>()}, {AllStatuses}."];
                }
            }

            MeasurementType? parsedType = null;

            if (measurementType is not null || Request.Query.ContainsKey("measurementType"))
            {
                if (TryParseEnumName<MeasurementType>(measurementType, out var typeValue))
                {
                    parsedType = typeValue;
                }
                else
                {
                    errors["measurementType"] = [$"Вкажіть один із допустимих типів: {AllowedValues<MeasurementType>()}."];
                }
            }

            if (page < 1)
            {
                errors["page"] = ["Номер сторінки має бути не меншим за 1."];
            }

            if (pageSize is < 1 or > WarningService.MaxPageSize)
            {
                errors["pageSize"] = [$"Розмір сторінки має бути від 1 до {WarningService.MaxPageSize}."];
            }

            if (errors.Count > 0)
            {
                return ValidationFailed(errors);
            }

            var criteria = new WarningSearchCriteria(
                equipmentId,
                parsedStatus,
                parsedType,
                page ?? 1,
                pageSize ?? WarningService.DefaultPageSize);

            try
            {
                return Ok(await _warningService.SearchAsync(criteria, cancellationToken));
            }
            catch (EquipmentNotFoundException)
            {
                return EquipmentNotFound();
            }
        }

        [HttpGet("{warningId:guid}")]
        [ProducesResponseType<WarningDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WarningDto>> GetAsync(Guid warningId, CancellationToken cancellationToken)
        {
            var warning = await _warningService.GetAsync(warningId, cancellationToken);
            return warning is null ? WarningNotFound() : Ok(warning);
        }

        [HttpPost("{warningId:guid}/acknowledge")]
        [ProducesResponseType<WarningDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WarningDto>> AcknowledgeAsync(Guid warningId, CancellationToken cancellationToken)
        {
            var warning = await _warningService.AcknowledgeAsync(warningId, cancellationToken);
            return warning is null ? WarningNotFound() : Ok(warning);
        }

        private ObjectResult WarningNotFound()
        {
            return ProblemResult(
                StatusCodes.Status404NotFound,
                "Попередження не знайдено.",
                new Dictionary<string, string[]> { ["warningId"] = ["Попередження з таким ідентифікатором не знайдено."] });
        }
    }
}
