using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.DTOs.Threshold;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Application.Mapping;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Domain.Services;

namespace EquipmentMonitoring.Application.Services
{
    /// <summary>
    /// Manages thresholds of both scopes: <c>equipmentId == null</c> addresses the global threshold.
    /// </summary>
    public class ThresholdService
    {
        private readonly IThresholdRepository _thresholdRepository;
        private readonly IEquipmentRepository _equipmentRepository;
        private readonly TimeProvider _timeProvider;

        public ThresholdService(
            IThresholdRepository thresholdRepository,
            IEquipmentRepository equipmentRepository,
            TimeProvider timeProvider)
        {
            _thresholdRepository = thresholdRepository;
            _equipmentRepository = equipmentRepository;
            _timeProvider = timeProvider;
        }

        public async Task<IReadOnlyList<ThresholdDto>> ListAsync(
            Guid? equipmentId,
            CancellationToken cancellationToken = default)
        {
            await EnsureEquipmentExistsAsync(equipmentId, cancellationToken);

            var thresholds = await _thresholdRepository.ListAsync(equipmentId, cancellationToken);
            return thresholds.Select(t => t.ToDto()).ToList();
        }

        public async Task<ThresholdDto?> GetAsync(
            Guid? equipmentId,
            MeasurementType measurementType,
            CancellationToken cancellationToken = default)
        {
            await EnsureEquipmentExistsAsync(equipmentId, cancellationToken);

            var threshold = await _thresholdRepository.FindAsync(equipmentId, measurementType, cancellationToken);
            return threshold?.ToDto();
        }

        public async Task<IReadOnlyList<ThresholdDto>> GetEffectiveAsync(
            Guid equipmentId,
            CancellationToken cancellationToken = default)
        {
            await EnsureEquipmentExistsAsync(equipmentId, cancellationToken);

            var candidates = await _thresholdRepository.ListApplicableAsync(equipmentId, null, cancellationToken);
            return ThresholdPrecedence.SelectEffectiveForEachType(candidates, equipmentId)
                .Select(t => t.ToDto())
                .ToList();
        }

        /// <remarks>Expects <paramref name="dto"/> to be validated by <c>UpsertThresholdValidator</c>.</remarks>
        public async Task<ThresholdUpsertResult> UpsertAsync(
            Guid? equipmentId,
            MeasurementType measurementType,
            UpsertThresholdDto dto,
            CancellationToken cancellationToken = default)
        {
            await EnsureEquipmentExistsAsync(equipmentId, cancellationToken);

            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var threshold = await _thresholdRepository.FindAsync(equipmentId, measurementType, cancellationToken);
            var created = threshold is null;

            if (threshold is null)
            {
                threshold = equipmentId is null
                    ? Threshold.CreateGlobal(measurementType, dto.MinValue, dto.MaxValue, nowUtc)
                    : Threshold.CreateForEquipment(equipmentId.Value, measurementType, dto.MinValue, dto.MaxValue, nowUtc);

                await _thresholdRepository.AddAsync(threshold, cancellationToken);
            }
            else
            {
                threshold.ChangeLimits(dto.MinValue, dto.MaxValue, nowUtc);
            }

            await _thresholdRepository.SaveChangesAsync(cancellationToken);

            return new ThresholdUpsertResult(threshold.ToDto(), created);
        }

        public async Task<bool> DeleteAsync(
            Guid? equipmentId,
            MeasurementType measurementType,
            CancellationToken cancellationToken = default)
        {
            await EnsureEquipmentExistsAsync(equipmentId, cancellationToken);

            var threshold = await _thresholdRepository.FindAsync(equipmentId, measurementType, cancellationToken);

            if (threshold is null)
            {
                return false;
            }

            _thresholdRepository.Remove(threshold);
            await _thresholdRepository.SaveChangesAsync(cancellationToken);

            return true;
        }

        private async Task EnsureEquipmentExistsAsync(Guid? equipmentId, CancellationToken cancellationToken)
        {
            if (equipmentId is not null
                && !await _equipmentRepository.ExistsByIdAsync(equipmentId.Value, cancellationToken))
            {
                throw new EquipmentNotFoundException(equipmentId.Value);
            }
        }
    }

    public sealed record ThresholdUpsertResult(ThresholdDto Threshold, bool Created);
}
