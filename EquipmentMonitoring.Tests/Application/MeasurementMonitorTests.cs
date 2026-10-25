using EquipmentMonitoring.Application.Monitoring;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace EquipmentMonitoring.Tests.Application
{
    public class MeasurementMonitorTests
    {
        private static readonly DateTime NowUtc = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

        private readonly InMemoryWarningRepository _warnings = new();

        [Fact]
        public async Task InspectAsync_ShouldStageWarningsFromAllRulesWithoutSaving()
        {
            var measurement = new Measurement(Guid.NewGuid(), MeasurementType.Load, 99, "%", NowUtc);
            var first = CreateWarning(measurement, 90);
            var second = CreateWarning(measurement, 95);
            var monitor = CreateMonitor(new StubRule(first), new StubRule(), new StubRule(second));

            var result = await monitor.InspectAsync(measurement);

            Assert.Equal([first, second], result);
            Assert.Equal([first, second], _warnings.Added);
            Assert.Equal(0, _warnings.SaveChangesCallsCount);
        }

        [Fact]
        public async Task InspectAsync_WithoutRules_ShouldReturnEmpty()
        {
            var monitor = CreateMonitor();

            var result = await monitor.InspectAsync(new Measurement(Guid.NewGuid(), MeasurementType.Load, 99, "%", NowUtc));

            Assert.Empty(result);
            Assert.Empty(_warnings.Added);
        }

        [Fact]
        public async Task InspectAsync_ShouldPassMeasurementAndCancellationTokenToRules()
        {
            var measurement = new Measurement(Guid.NewGuid(), MeasurementType.Voltage, 230, "V", NowUtc);
            var rule = new StubRule();
            using var cancellation = new CancellationTokenSource();

            await CreateMonitor(rule).InspectAsync(measurement, cancellation.Token);

            Assert.Same(measurement, rule.ReceivedMeasurement);
            Assert.Equal(cancellation.Token, rule.ReceivedToken);
        }

        private MeasurementMonitor CreateMonitor(params IMeasurementRule[] rules)
        {
            return new MeasurementMonitor(rules, _warnings, NullLogger<MeasurementMonitor>.Instance);
        }

        private static Warning CreateWarning(Measurement measurement, double max)
        {
            var threshold = Threshold.CreateGlobal(measurement.Type, null, max, NowUtc);
            return Warning.FromBreach(measurement, threshold, threshold.Evaluate(measurement.Value)!, NowUtc);
        }

        private sealed class StubRule : IMeasurementRule
        {
            private readonly Warning[] _warnings;

            public StubRule(params Warning[] warnings)
            {
                _warnings = warnings;
            }

            public Measurement? ReceivedMeasurement { get; private set; }

            public CancellationToken ReceivedToken { get; private set; }

            public Task<IReadOnlyList<Warning>> EvaluateAsync(Measurement measurement, CancellationToken cancellationToken = default)
            {
                ReceivedMeasurement = measurement;
                ReceivedToken = cancellationToken;
                return Task.FromResult<IReadOnlyList<Warning>>(_warnings);
            }
        }
    }
}
