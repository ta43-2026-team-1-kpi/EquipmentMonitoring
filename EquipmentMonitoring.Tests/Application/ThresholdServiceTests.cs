using EquipmentMonitoring.Application.DTOs.Threshold;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Application.Services;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using EquipmentMonitoring.Tests.TestDoubles;

namespace EquipmentMonitoring.Tests.Application
{
    public class ThresholdServiceTests
    {
        private static readonly DateTime NowUtc = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
        private static readonly Guid EquipmentId = Guid.NewGuid();

        private readonly InMemoryThresholdRepository _thresholds = new();
        private readonly StubEquipmentRepository _equipment = new();
        private readonly FixedTimeProvider _time = new(NowUtc);
        private readonly ThresholdService _service;

        public ThresholdServiceTests()
        {
            _equipment.ExistingIds.Add(EquipmentId);
            _service = new ThresholdService(_thresholds, _equipment, _time);
        }

        [Fact]
        public async Task UpsertAsync_WhenMissing_ShouldCreateGlobalThreshold()
        {
            var result = await _service.UpsertAsync(null, MeasurementType.Temperature, new UpsertThresholdDto { MinValue = -10, MaxValue = 80 });

            Assert.True(result.Created);
            Assert.Equal(ThresholdScope.Global, result.Threshold.Scope);
            Assert.Equal(-10, result.Threshold.MinValue);
            Assert.Equal(80, result.Threshold.MaxValue);
            Assert.Single(_thresholds.Thresholds);
            Assert.Equal(1, _thresholds.SaveChangesCallsCount);
        }

        [Fact]
        public async Task UpsertAsync_WhenExisting_ShouldUpdateLimitsInPlace()
        {
            var existing = Threshold.CreateForEquipment(EquipmentId, MeasurementType.Load, null, 90, NowUtc);
            _thresholds.Thresholds.Add(existing);
            _time.Advance(TimeSpan.FromHours(1));

            var result = await _service.UpsertAsync(EquipmentId, MeasurementType.Load, new UpsertThresholdDto { MinValue = 5, MaxValue = 85 });

            Assert.False(result.Created);
            Assert.Equal(existing.Id, result.Threshold.Id);
            Assert.Equal(5, existing.MinValue);
            Assert.Equal(85, existing.MaxValue);
            Assert.Equal(NowUtc.AddHours(1), existing.UpdatedAtUtc);
            Assert.Single(_thresholds.Thresholds);
        }

        [Fact]
        public async Task UpsertAsync_ForUnknownEquipment_ShouldThrowWithoutSaving()
        {
            var unknownId = Guid.NewGuid();

            var exception = await Assert.ThrowsAsync<EquipmentNotFoundException>(() =>
                _service.UpsertAsync(unknownId, MeasurementType.Load, new UpsertThresholdDto { MaxValue = 90 }));

            Assert.Equal(unknownId, exception.EquipmentId);
            Assert.Empty(_thresholds.Thresholds);
            Assert.Equal(0, _thresholds.SaveChangesCallsCount);
        }

        [Fact]
        public async Task GetAsync_ShouldReturnThresholdOfExactScopeOnly()
        {
            _thresholds.Thresholds.Add(Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc));

            Assert.NotNull(await _service.GetAsync(null, MeasurementType.Load));
            Assert.Null(await _service.GetAsync(EquipmentId, MeasurementType.Load));
        }

        [Fact]
        public async Task GetAsync_ForUnknownEquipment_ShouldThrow()
        {
            await Assert.ThrowsAsync<EquipmentNotFoundException>(() => _service.GetAsync(Guid.NewGuid(), MeasurementType.Load));
        }

        [Fact]
        public async Task ListAsync_ShouldReturnRequestedScope()
        {
            _thresholds.Thresholds.Add(Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc));
            _thresholds.Thresholds.Add(Threshold.CreateForEquipment(EquipmentId, MeasurementType.Voltage, 200, 240, NowUtc));

            var globals = await _service.ListAsync(null);
            var forEquipment = await _service.ListAsync(EquipmentId);

            Assert.Equal(MeasurementType.Load, Assert.Single(globals).MeasurementType);
            Assert.Equal(MeasurementType.Voltage, Assert.Single(forEquipment).MeasurementType);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveExistingThreshold()
        {
            _thresholds.Thresholds.Add(Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc));

            Assert.True(await _service.DeleteAsync(null, MeasurementType.Load));
            Assert.Empty(_thresholds.Thresholds);
            Assert.Equal(1, _thresholds.SaveChangesCallsCount);
        }

        [Fact]
        public async Task DeleteAsync_WhenMissing_ShouldReturnFalseWithoutSaving()
        {
            Assert.False(await _service.DeleteAsync(EquipmentId, MeasurementType.Load));
            Assert.Equal(0, _thresholds.SaveChangesCallsCount);
        }

        [Fact]
        public async Task GetEffectiveAsync_ShouldMergeGlobalAndEquipmentThresholds()
        {
            _thresholds.Thresholds.Add(Threshold.CreateGlobal(MeasurementType.Temperature, -10, 80, NowUtc));
            _thresholds.Thresholds.Add(Threshold.CreateGlobal(MeasurementType.Load, null, 90, NowUtc));
            _thresholds.Thresholds.Add(Threshold.CreateForEquipment(EquipmentId, MeasurementType.Load, null, 70, NowUtc));

            var effective = await _service.GetEffectiveAsync(EquipmentId);

            Assert.Equal(2, effective.Count);
            Assert.Equal(ThresholdScope.Global, effective.Single(t => t.MeasurementType == MeasurementType.Temperature).Scope);
            Assert.Equal(70, effective.Single(t => t.MeasurementType == MeasurementType.Load).MaxValue);
        }

        [Fact]
        public async Task GetEffectiveAsync_ForUnknownEquipment_ShouldThrow()
        {
            await Assert.ThrowsAsync<EquipmentNotFoundException>(() => _service.GetEffectiveAsync(Guid.NewGuid()));
        }
    }
}
