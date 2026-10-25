using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Tests.Domain
{
    public class ThresholdTests
    {
        private static readonly DateTime NowUtc = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void CreateGlobal_WithValidLimits_ShouldCreateGlobalThreshold()
        {
            var threshold = Threshold.CreateGlobal(MeasurementType.Temperature, -10, 80, NowUtc);

            Assert.NotEqual(Guid.Empty, threshold.Id);
            Assert.Null(threshold.EquipmentId);
            Assert.Equal(ThresholdScope.Global, threshold.Scope);
            Assert.Equal(MeasurementType.Temperature, threshold.MeasurementType);
            Assert.Equal(-10, threshold.MinValue);
            Assert.Equal(80, threshold.MaxValue);
            Assert.Equal(NowUtc, threshold.CreatedAtUtc);
            Assert.Equal(NowUtc, threshold.UpdatedAtUtc);
        }

        [Fact]
        public void CreateForEquipment_WithValidLimits_ShouldCreateEquipmentThreshold()
        {
            var equipmentId = Guid.NewGuid();

            var threshold = Threshold.CreateForEquipment(equipmentId, MeasurementType.Voltage, 210, 240, NowUtc);

            Assert.Equal(equipmentId, threshold.EquipmentId);
            Assert.Equal(ThresholdScope.Equipment, threshold.Scope);
        }

        [Theory]
        [InlineData(null, 80.0)]
        [InlineData(-10.0, null)]
        [InlineData(50.0, 50.0)]
        public void CreateGlobal_WithOneSidedOrEqualLimits_ShouldBeAllowed(double? min, double? max)
        {
            var threshold = Threshold.CreateGlobal(MeasurementType.Load, min, max, NowUtc);

            Assert.Equal(min, threshold.MinValue);
            Assert.Equal(max, threshold.MaxValue);
        }

        [Fact]
        public void CreateGlobal_WithoutAnyLimit_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(() =>
                Threshold.CreateGlobal(MeasurementType.Load, null, null, NowUtc));
        }

        [Fact]
        public void CreateGlobal_WithMinGreaterThanMax_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(() =>
                Threshold.CreateGlobal(MeasurementType.Load, 90, 10, NowUtc));
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void CreateGlobal_WithNonFiniteLimit_ShouldThrow(double limit)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Threshold.CreateGlobal(MeasurementType.Load, limit, null, NowUtc));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Threshold.CreateGlobal(MeasurementType.Load, null, limit, NowUtc));
        }

        [Fact]
        public void CreateGlobal_WithUndefinedMeasurementType_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Threshold.CreateGlobal((MeasurementType)42, 0, 1, NowUtc));
        }

        [Fact]
        public void CreateForEquipment_WithEmptyEquipmentId_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(() =>
                Threshold.CreateForEquipment(Guid.Empty, MeasurementType.Load, 0, 1, NowUtc));
        }

        [Fact]
        public void CreateGlobal_WithNonUtcTimestamp_ShouldThrow()
        {
            var localNow = DateTime.SpecifyKind(NowUtc, DateTimeKind.Local);

            Assert.Throws<ArgumentException>(() =>
                Threshold.CreateGlobal(MeasurementType.Load, 0, 1, localNow));
        }

        [Fact]
        public void ChangeLimits_ShouldReplaceLimitsAndTouchUpdatedAt()
        {
            var threshold = Threshold.CreateGlobal(MeasurementType.Temperature, -10, 80, NowUtc);
            var later = NowUtc.AddHours(1);

            threshold.ChangeLimits(null, 95, later);

            Assert.Null(threshold.MinValue);
            Assert.Equal(95, threshold.MaxValue);
            Assert.Equal(NowUtc, threshold.CreatedAtUtc);
            Assert.Equal(later, threshold.UpdatedAtUtc);
        }

        [Fact]
        public void ChangeLimits_WithInvalidLimits_ShouldThrowAndKeepPreviousLimits()
        {
            var threshold = Threshold.CreateGlobal(MeasurementType.Temperature, -10, 80, NowUtc);

            Assert.Throws<ArgumentException>(() => threshold.ChangeLimits(100, 0, NowUtc.AddHours(1)));

            Assert.Equal(-10, threshold.MinValue);
            Assert.Equal(80, threshold.MaxValue);
            Assert.Equal(NowUtc, threshold.UpdatedAtUtc);
        }

        [Theory]
        [InlineData(-10.0)]
        [InlineData(0.0)]
        [InlineData(80.0)]
        public void Evaluate_WithValueWithinLimitsInclusive_ShouldReturnNull(double value)
        {
            var threshold = Threshold.CreateGlobal(MeasurementType.Temperature, -10, 80, NowUtc);

            Assert.Null(threshold.Evaluate(value));
        }

        [Fact]
        public void Evaluate_WithValueAboveMaximum_ShouldReturnAboveMaximumBreach()
        {
            var threshold = Threshold.CreateGlobal(MeasurementType.Temperature, -10, 80, NowUtc);

            var breach = threshold.Evaluate(80.01);

            Assert.NotNull(breach);
            Assert.Equal(ThresholdBreachType.AboveMaximum, breach.Type);
            Assert.Equal(80, breach.Limit);
        }

        [Fact]
        public void Evaluate_WithValueBelowMinimum_ShouldReturnBelowMinimumBreach()
        {
            var threshold = Threshold.CreateGlobal(MeasurementType.Temperature, -10, 80, NowUtc);

            var breach = threshold.Evaluate(-10.5);

            Assert.NotNull(breach);
            Assert.Equal(ThresholdBreachType.BelowMinimum, breach.Type);
            Assert.Equal(-10, breach.Limit);
        }

        [Fact]
        public void Evaluate_WithOnlyMaximum_ShouldIgnoreLowValues()
        {
            var threshold = Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc);

            Assert.Null(threshold.Evaluate(-1_000_000));
            Assert.Equal(ThresholdBreachType.AboveMaximum, threshold.Evaluate(90.1)!.Type);
        }

        [Fact]
        public void Evaluate_WithOnlyMinimum_ShouldIgnoreHighValues()
        {
            var threshold = Threshold.CreateGlobal(MeasurementType.Voltage, 200, null, NowUtc);

            Assert.Null(threshold.Evaluate(1_000_000));
            Assert.Equal(ThresholdBreachType.BelowMinimum, threshold.Evaluate(199.9)!.Type);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        public void Evaluate_WithNonFiniteValue_ShouldThrow(double value)
        {
            var threshold = Threshold.CreateGlobal(MeasurementType.Load, 0, 100, NowUtc);

            Assert.Throws<ArgumentOutOfRangeException>(() => threshold.Evaluate(value));
        }

        [Fact]
        public void AppliesTo_ShouldMatchTypeAndScope()
        {
            var equipmentId = Guid.NewGuid();
            var otherEquipmentId = Guid.NewGuid();
            var global = Threshold.CreateGlobal(MeasurementType.Load, 0, 100, NowUtc);
            var specific = Threshold.CreateForEquipment(equipmentId, MeasurementType.Load, 0, 100, NowUtc);

            Assert.True(global.AppliesTo(equipmentId, MeasurementType.Load));
            Assert.True(global.AppliesTo(otherEquipmentId, MeasurementType.Load));
            Assert.False(global.AppliesTo(equipmentId, MeasurementType.Voltage));
            Assert.True(specific.AppliesTo(equipmentId, MeasurementType.Load));
            Assert.False(specific.AppliesTo(otherEquipmentId, MeasurementType.Load));
        }
    }
}
