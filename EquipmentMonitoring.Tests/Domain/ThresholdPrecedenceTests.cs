using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Domain.Services;

namespace EquipmentMonitoring.Tests.Domain
{
    public class ThresholdPrecedenceTests
    {
        private static readonly DateTime NowUtc = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
        private static readonly Guid EquipmentId = Guid.NewGuid();

        [Fact]
        public void SelectEffective_WithoutCandidates_ShouldReturnNull()
        {
            Assert.Null(ThresholdPrecedence.SelectEffective([], EquipmentId, MeasurementType.Load));
        }

        [Fact]
        public void SelectEffective_WithOnlyGlobal_ShouldReturnGlobal()
        {
            var global = Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc);

            Assert.Same(global, ThresholdPrecedence.SelectEffective([global], EquipmentId, MeasurementType.Load));
        }

        [Fact]
        public void SelectEffective_WithEquipmentAndGlobal_ShouldPreferEquipmentRegardlessOfOrder()
        {
            var global = Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc);
            var specific = Threshold.CreateForEquipment(EquipmentId, MeasurementType.Load, null, 70, NowUtc);

            Assert.Same(specific, ThresholdPrecedence.SelectEffective([global, specific], EquipmentId, MeasurementType.Load));
            Assert.Same(specific, ThresholdPrecedence.SelectEffective([specific, global], EquipmentId, MeasurementType.Load));
        }

        [Fact]
        public void SelectEffective_ShouldIgnoreOtherEquipmentAndOtherTypes()
        {
            var otherEquipment = Threshold.CreateForEquipment(Guid.NewGuid(), MeasurementType.Load, null, 10, NowUtc);
            var otherType = Threshold.CreateForEquipment(EquipmentId, MeasurementType.Voltage, 200, 240, NowUtc);
            var global = Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc);

            var effective = ThresholdPrecedence.SelectEffective([otherEquipment, otherType, global], EquipmentId, MeasurementType.Load);

            Assert.Same(global, effective);
        }

        [Fact]
        public void SelectEffectiveForEachType_ShouldResolveEveryTypeIndependently()
        {
            var globalTemperature = Threshold.CreateGlobal(MeasurementType.Temperature, -10, 80, NowUtc);
            var globalLoad = Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc);
            var specificLoad = Threshold.CreateForEquipment(EquipmentId, MeasurementType.Load, null, 70, NowUtc);

            var effective = ThresholdPrecedence.SelectEffectiveForEachType([globalTemperature, globalLoad, specificLoad], EquipmentId);

            Assert.Equal(2, effective.Count);
            Assert.Same(globalTemperature, effective.Single(t => t.MeasurementType == MeasurementType.Temperature));
            Assert.Same(specificLoad, effective.Single(t => t.MeasurementType == MeasurementType.Load));
            Assert.DoesNotContain(effective, t => t.MeasurementType == MeasurementType.Voltage);
        }
    }
}
