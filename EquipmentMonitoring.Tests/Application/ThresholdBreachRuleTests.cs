using EquipmentMonitoring.Application.Monitoring;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace EquipmentMonitoring.Tests.Application
{
    public class ThresholdBreachRuleTests
    {
        private static readonly DateTime NowUtc = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
        private static readonly Guid EquipmentId = Guid.NewGuid();

        private readonly InMemoryThresholdRepository _thresholds = new();
        private readonly ThresholdBreachRule _rule;

        public ThresholdBreachRuleTests()
        {
            _rule = new ThresholdBreachRule(_thresholds, new FixedTimeProvider(NowUtc), NullLogger<ThresholdBreachRule>.Instance);
        }

        [Fact]
        public async Task EvaluateAsync_WithoutThreshold_ShouldReturnNoWarnings()
        {
            var warnings = await _rule.EvaluateAsync(CreateMeasurement(MeasurementType.Temperature, 1_000));

            Assert.Empty(warnings);
        }

        [Theory]
        [InlineData(-10.0)]
        [InlineData(25.0)]
        [InlineData(80.0)]
        public async Task EvaluateAsync_WithValueWithinLimits_ShouldReturnNoWarnings(double value)
        {
            _thresholds.Thresholds.Add(Threshold.CreateGlobal(MeasurementType.Temperature, -10, 80, NowUtc));

            var warnings = await _rule.EvaluateAsync(CreateMeasurement(MeasurementType.Temperature, value));

            Assert.Empty(warnings);
        }

        [Fact]
        public async Task EvaluateAsync_WithValueAboveMaximum_ShouldReturnAboveMaximumWarning()
        {
            var threshold = Threshold.CreateGlobal(MeasurementType.Temperature, -10, 80, NowUtc);
            _thresholds.Thresholds.Add(threshold);
            var measurement = CreateMeasurement(MeasurementType.Temperature, 85);

            var warning = Assert.Single(await _rule.EvaluateAsync(measurement));

            Assert.Equal(ThresholdBreachType.AboveMaximum, warning.BreachType);
            Assert.Equal(80, warning.LimitValue);
            Assert.Equal(85, warning.ActualValue);
            Assert.Equal(measurement.Id, warning.MeasurementId);
            Assert.Equal(threshold.Id, warning.ThresholdId);
            Assert.Equal(NowUtc, warning.CreatedAtUtc);
        }

        [Fact]
        public async Task EvaluateAsync_WithValueBelowMinimum_ShouldReturnBelowMinimumWarning()
        {
            _thresholds.Thresholds.Add(Threshold.CreateGlobal(MeasurementType.Voltage, 200, 240, NowUtc));

            var warning = Assert.Single(await _rule.EvaluateAsync(CreateMeasurement(MeasurementType.Voltage, 180)));

            Assert.Equal(ThresholdBreachType.BelowMinimum, warning.BreachType);
            Assert.Equal(200, warning.LimitValue);
        }

        [Fact]
        public async Task EvaluateAsync_ShouldUseThresholdOfMeasurementTypeOnly()
        {
            _thresholds.Thresholds.Add(Threshold.CreateGlobal(MeasurementType.Load, null, 50, NowUtc));

            var warnings = await _rule.EvaluateAsync(CreateMeasurement(MeasurementType.Temperature, 75));

            Assert.Empty(warnings);
        }

        [Fact]
        public async Task EvaluateAsync_WithEquipmentThreshold_ShouldOverrideGlobal()
        {
            _thresholds.Thresholds.Add(Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc));
            _thresholds.Thresholds.Add(Threshold.CreateForEquipment(EquipmentId, MeasurementType.Load, null, 70, NowUtc));

            var warning = Assert.Single(await _rule.EvaluateAsync(CreateMeasurement(MeasurementType.Load, 75)));

            Assert.Equal(70, warning.LimitValue);
            Assert.Equal(ThresholdScope.Equipment, warning.ThresholdScope);
        }

        [Fact]
        public async Task EvaluateAsync_WithLooserEquipmentThreshold_ShouldNotFallBackToGlobal()
        {
            _thresholds.Thresholds.Add(Threshold.CreateGlobal(MeasurementType.Load, null, 70, NowUtc));
            _thresholds.Thresholds.Add(Threshold.CreateForEquipment(EquipmentId, MeasurementType.Load, null, 95, NowUtc));

            var warnings = await _rule.EvaluateAsync(CreateMeasurement(MeasurementType.Load, 80));

            Assert.Empty(warnings);
        }

        [Fact]
        public async Task EvaluateAsync_WithUnitNotMatchingType_ShouldSkipEvaluation()
        {
            _thresholds.Thresholds.Add(Threshold.CreateGlobal(MeasurementType.Temperature, -10, 80, NowUtc));
            var fahrenheit = new Measurement(EquipmentId, MeasurementType.Temperature, 150, "°F", NowUtc);

            var warnings = await _rule.EvaluateAsync(fahrenheit);

            Assert.Empty(warnings);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        public async Task EvaluateAsync_WithNonFiniteValue_ShouldSkipEvaluation(double value)
        {
            _thresholds.Thresholds.Add(Threshold.CreateGlobal(MeasurementType.Load, 0, 100, NowUtc));

            var warnings = await _rule.EvaluateAsync(CreateMeasurement(MeasurementType.Load, value));

            Assert.Empty(warnings);
        }

        [Fact]
        public async Task EvaluateAsync_WithUndefinedMeasurementType_ShouldSkipEvaluation()
        {
            var measurement = new Measurement(EquipmentId, (MeasurementType)42, 1, "?", NowUtc);

            var warnings = await _rule.EvaluateAsync(measurement);

            Assert.Empty(warnings);
        }

        private static Measurement CreateMeasurement(MeasurementType type, double value)
        {
            return new Measurement(EquipmentId, type, value, type.GetUnit(), NowUtc.AddMinutes(-1));
        }
    }
}
