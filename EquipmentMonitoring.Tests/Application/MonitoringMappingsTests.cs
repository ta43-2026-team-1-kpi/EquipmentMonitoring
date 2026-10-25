using EquipmentMonitoring.Application.Mapping;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Tests.Application
{
    public class MonitoringMappingsTests
    {
        private static readonly DateTime NowUtc = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

        [Theory]
        [InlineData(MeasurementType.Temperature, "°C")]
        [InlineData(MeasurementType.Voltage, "V")]
        [InlineData(MeasurementType.Load, "%")]
        public void ThresholdToDto_ShouldExposeCanonicalUnit(MeasurementType type, string expectedUnit)
        {
            var dto = Threshold.CreateGlobal(type, 0, 1, NowUtc).ToDto();

            Assert.Equal(expectedUnit, dto.Unit);
        }

        [Fact]
        public void ThresholdToDto_ShouldMapAllFieldsAsUtc()
        {
            var equipmentId = Guid.NewGuid();
            var threshold = Threshold.CreateForEquipment(equipmentId, MeasurementType.Voltage, null, 240, NowUtc);

            var dto = threshold.ToDto();

            Assert.Equal(threshold.Id, dto.Id);
            Assert.Equal(ThresholdScope.Equipment, dto.Scope);
            Assert.Equal(equipmentId, dto.EquipmentId);
            Assert.Null(dto.MinValue);
            Assert.Equal(240, dto.MaxValue);
            Assert.Equal(TimeSpan.Zero, dto.CreatedAtUtc.Offset);
            Assert.Equal(NowUtc, dto.UpdatedAtUtc.UtcDateTime);
        }

        [Fact]
        public void GetUnit_WithUndefinedType_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ((MeasurementType)42).GetUnit());
        }
    }
}
