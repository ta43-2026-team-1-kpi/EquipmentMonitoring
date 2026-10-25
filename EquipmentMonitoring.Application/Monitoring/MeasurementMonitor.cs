using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EquipmentMonitoring.Application.Monitoring
{
    public class MeasurementMonitor : IMeasurementMonitor
    {
        private readonly IEnumerable<IMeasurementRule> _rules;
        private readonly IWarningRepository _warningRepository;
        private readonly ILogger<MeasurementMonitor> _logger;

        public MeasurementMonitor(
            IEnumerable<IMeasurementRule> rules,
            IWarningRepository warningRepository,
            ILogger<MeasurementMonitor> logger)
        {
            _rules = rules;
            _warningRepository = warningRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Warning>> InspectAsync(
            Measurement measurement,
            CancellationToken cancellationToken = default)
        {
            var warnings = new List<Warning>();

            foreach (var rule in _rules)
            {
                warnings.AddRange(await rule.EvaluateAsync(measurement, cancellationToken));
            }

            foreach (var warning in warnings)
            {
                await _warningRepository.AddAsync(warning, cancellationToken);

                _logger.LogInformation(
                    "Warning {WarningId} raised for equipment {EquipmentId}: {MeasurementType} {ActualValue} {Unit} is {BreachType} limit {LimitValue}.",
                    warning.Id,
                    warning.EquipmentId,
                    warning.MeasurementType,
                    warning.ActualValue,
                    warning.Unit,
                    warning.BreachType,
                    warning.LimitValue);
            }

            return warnings;
        }
    }
}
