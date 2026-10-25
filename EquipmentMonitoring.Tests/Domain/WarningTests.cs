using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Domain.ValueObjects;

namespace EquipmentMonitoring.Tests.Domain
{
    public class WarningTests
    {
        private static readonly DateTime MeasuredAtUtc = new(2026, 10, 25, 11, 55, 0, DateTimeKind.Utc);
        private static readonly DateTime NowUtc = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void FromBreach_ShouldSnapshotMeasurementAndThresholdData()
        {
            var equipmentId = Guid.NewGuid();
            var measurement = new Measurement(equipmentId, MeasurementType.Temperature, 92.5, "°C", MeasuredAtUtc);
            var threshold = Threshold.CreateForEquipment(equipmentId, MeasurementType.Temperature, -10, 80, NowUtc);
            var breach = threshold.Evaluate(measurement.Value)!;

            var warning = Warning.FromBreach(measurement, threshold, breach, NowUtc);

            Assert.NotEqual(Guid.Empty, warning.Id);
            Assert.Equal(equipmentId, warning.EquipmentId);
            Assert.Equal(measurement.Id, warning.MeasurementId);
            Assert.Equal(threshold.Id, warning.ThresholdId);
            Assert.Equal(ThresholdScope.Equipment, warning.ThresholdScope);
            Assert.Equal(MeasurementType.Temperature, warning.MeasurementType);
            Assert.Equal(92.5, warning.ActualValue);
            Assert.Equal("°C", warning.Unit);
            Assert.Equal(ThresholdBreachType.AboveMaximum, warning.BreachType);
            Assert.Equal(80, warning.LimitValue);
            Assert.Equal(MeasuredAtUtc, warning.MeasuredAtUtc);
            Assert.Equal(NowUtc, warning.CreatedAtUtc);
            Assert.Equal(WarningStatus.Active, warning.Status);
            Assert.Null(warning.AcknowledgedAtUtc);
        }

        [Fact]
        public void FromBreach_ShouldNotDependOnLaterThresholdChanges()
        {
            var measurement = new Measurement(Guid.NewGuid(), MeasurementType.Voltage, 190, "V", MeasuredAtUtc);
            var threshold = Threshold.CreateGlobal(MeasurementType.Voltage, 200, 240, NowUtc);
            var warning = Warning.FromBreach(measurement, threshold, threshold.Evaluate(190)!, NowUtc);

            threshold.ChangeLimits(150, 240, NowUtc.AddMinutes(1));

            Assert.Equal(200, warning.LimitValue);
            Assert.Equal(ThresholdBreachType.BelowMinimum, warning.BreachType);
        }

        [Fact]
        public void FromBreach_WithThresholdOfAnotherType_ShouldThrow()
        {
            var measurement = new Measurement(Guid.NewGuid(), MeasurementType.Temperature, 100, "°C", MeasuredAtUtc);
            var threshold = Threshold.CreateGlobal(MeasurementType.Load, 0, 90, NowUtc);

            Assert.Throws<ArgumentException>(() =>
                Warning.FromBreach(measurement, threshold, new ThresholdBreach(ThresholdBreachType.AboveMaximum, 90), NowUtc));
        }

        [Fact]
        public void FromBreach_WithThresholdOfAnotherEquipment_ShouldThrow()
        {
            var measurement = new Measurement(Guid.NewGuid(), MeasurementType.Load, 100, "%", MeasuredAtUtc);
            var threshold = Threshold.CreateForEquipment(Guid.NewGuid(), MeasurementType.Load, 0, 90, NowUtc);

            Assert.Throws<ArgumentException>(() =>
                Warning.FromBreach(measurement, threshold, threshold.Evaluate(100)!, NowUtc));
        }

        [Fact]
        public void Acknowledge_WhenActive_ShouldMarkAsAcknowledged()
        {
            var warning = CreateWarning();
            var acknowledgedAt = NowUtc.AddMinutes(10);

            var changed = warning.Acknowledge(acknowledgedAt);

            Assert.True(changed);
            Assert.Equal(WarningStatus.Acknowledged, warning.Status);
            Assert.Equal(acknowledgedAt, warning.AcknowledgedAtUtc);
        }

        [Fact]
        public void Acknowledge_WhenAlreadyAcknowledged_ShouldKeepOriginalTimestamp()
        {
            var warning = CreateWarning();
            var firstAcknowledgedAt = NowUtc.AddMinutes(10);
            warning.Acknowledge(firstAcknowledgedAt);

            var changed = warning.Acknowledge(NowUtc.AddMinutes(20));

            Assert.False(changed);
            Assert.Equal(WarningStatus.Acknowledged, warning.Status);
            Assert.Equal(firstAcknowledgedAt, warning.AcknowledgedAtUtc);
        }

        [Fact]
        public void Acknowledge_WithNonUtcTimestamp_ShouldThrow()
        {
            var warning = CreateWarning();

            Assert.Throws<ArgumentException>(() =>
                warning.Acknowledge(DateTime.SpecifyKind(NowUtc, DateTimeKind.Unspecified)));
            Assert.Equal(WarningStatus.Active, warning.Status);
        }

        private static Warning CreateWarning()
        {
            var measurement = new Measurement(Guid.NewGuid(), MeasurementType.Load, 97, "%", MeasuredAtUtc);
            var threshold = Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc);

            return Warning.FromBreach(measurement, threshold, threshold.Evaluate(97)!, NowUtc);
        }
    }
}
