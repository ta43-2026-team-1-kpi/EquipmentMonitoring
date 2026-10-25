using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Domain.Services;
using Microsoft.Extensions.Logging;

namespace EquipmentMonitoring.Application.Monitoring
{
    public class ThresholdBreachRule : IMeasurementRule
    {
        private readonly IThresholdRepository _thresholdRepository;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<ThresholdBreachRule> _logger;

        public ThresholdBreachRule(
            IThresholdRepository thresholdRepository,
            TimeProvider timeProvider,
            ILogger<ThresholdBreachRule> logger)
        {
            _thresholdRepository = thresholdRepository;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Warning>> EvaluateAsync(
            Measurement measurement,
            CancellationToken cancellationToken = default)
        {
            if (!CanBeCompared(measurement))
            {
                return [];
            }

            var candidates = await _thresholdRepository.ListApplicableAsync(
                measurement.EquipmentId,
                measurement.Type,
                cancellationToken);

            var threshold = ThresholdPrecedence.SelectEffective(candidates, measurement.EquipmentId, measurement.Type);
            var breach = threshold?.Evaluate(measurement.Value);

            if (threshold is null || breach is null)
            {
                return [];
            }

            return [Warning.FromBreach(measurement, threshold, breach, _timeProvider.GetUtcNow().UtcDateTime)];
        }

        // Thresholds are stored in the canonical unit of each measurement type,
        // so anything else cannot be compared safely.
        private bool CanBeCompared(Measurement measurement)
        {
            if (!Enum.IsDefined(measurement.Type)
                || !double.IsFinite(measurement.Value)
                || !string.Equals(measurement.Unit, measurement.Type.GetUnit(), StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "Skipping threshold check for measurement {MeasurementId}: type {MeasurementType}, value {Value}, unit {Unit} cannot be compared with thresholds.",
                    measurement.Id,
                    measurement.Type,
                    measurement.Value,
                    measurement.Unit);

                return false;
            }

            return true;
        }
    }
}
