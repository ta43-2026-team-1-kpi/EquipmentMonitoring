using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.DTOs.Common;
using EquipmentMonitoring.Application.DTOs.Warning;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Application.Mapping;

namespace EquipmentMonitoring.Application.Services
{
    public class WarningService
    {
        public const int DefaultPageSize = 50;
        public const int MaxPageSize = 200;

        private readonly IWarningRepository _warningRepository;
        private readonly IEquipmentRepository _equipmentRepository;
        private readonly TimeProvider _timeProvider;

        public WarningService(
            IWarningRepository warningRepository,
            IEquipmentRepository equipmentRepository,
            TimeProvider timeProvider)
        {
            _warningRepository = warningRepository;
            _equipmentRepository = equipmentRepository;
            _timeProvider = timeProvider;
        }

        public async Task<PagedResultDto<WarningDto>> SearchAsync(
            WarningSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            if (criteria.EquipmentId is not null
                && !await _equipmentRepository.ExistsByIdAsync(criteria.EquipmentId.Value, cancellationToken))
            {
                throw new EquipmentNotFoundException(criteria.EquipmentId.Value);
            }

            var result = await _warningRepository.SearchAsync(criteria, cancellationToken);

            return new PagedResultDto<WarningDto>
            {
                Items = result.Items.Select(w => w.ToDto()).ToList(),
                Page = criteria.Page,
                PageSize = criteria.PageSize,
                TotalCount = result.TotalCount
            };
        }

        public Task<WarningSummaryDto> GetActiveSummaryAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<WarningDto?> GetAsync(Guid warningId, CancellationToken cancellationToken = default)
        {
            var warning = await _warningRepository.FindAsync(warningId, cancellationToken);
            return warning?.ToDto();
        }

        /// <summary>
        /// Idempotent: acknowledging an already acknowledged warning returns it unchanged.
        /// </summary>
        public async Task<WarningDto?> AcknowledgeAsync(Guid warningId, CancellationToken cancellationToken = default)
        {
            var warning = await _warningRepository.FindAsync(warningId, cancellationToken);

            if (warning is null)
            {
                return null;
            }

            if (warning.Acknowledge(_timeProvider.GetUtcNow().UtcDateTime))
            {
                try
                {
                    await _warningRepository.SaveChangesAsync(cancellationToken);
                }
                catch (ConcurrencyConflictException)
                {
                    // Another request acknowledged or removed it first; the entity is already refreshed.
                    warning = await _warningRepository.FindAsync(warningId, cancellationToken);
                }
            }

            return warning?.ToDto();
        }
    }
}
