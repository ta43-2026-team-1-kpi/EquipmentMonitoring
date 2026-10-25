using EquipmentMonitoring.Application.DTOs.Threshold;
using EquipmentMonitoring.Application.Validation;

namespace EquipmentMonitoring.Tests.Application
{
    public class UpsertThresholdValidatorTests
    {
        private readonly UpsertThresholdValidator _validator = new();

        [Theory]
        [InlineData(-10.0, 80.0)]
        [InlineData(null, 80.0)]
        [InlineData(-10.0, null)]
        [InlineData(5.0, 5.0)]
        public void Validate_WithValidLimits_ShouldReturnNoErrors(double? min, double? max)
        {
            var result = _validator.Validate(new UpsertThresholdDto { MinValue = min, MaxValue = max });

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithoutAnyLimit_ShouldReturnErrorForBothFields()
        {
            var result = _validator.Validate(new UpsertThresholdDto());

            Assert.False(result.IsValid);
            Assert.Contains(nameof(UpsertThresholdDto.MinValue), result.Errors.Keys);
            Assert.Contains(nameof(UpsertThresholdDto.MaxValue), result.Errors.Keys);
        }

        [Fact]
        public void Validate_WithMinGreaterThanMax_ShouldReturnErrors()
        {
            var result = _validator.Validate(new UpsertThresholdDto { MinValue = 90, MaxValue = 10 });

            Assert.False(result.IsValid);
            Assert.Contains(nameof(UpsertThresholdDto.MinValue), result.Errors.Keys);
            Assert.Contains(nameof(UpsertThresholdDto.MaxValue), result.Errors.Keys);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void Validate_WithNonFiniteValues_ShouldReturnFieldErrors(double value)
        {
            var result = _validator.Validate(new UpsertThresholdDto { MinValue = value, MaxValue = value });

            Assert.Equal(["Мінімальне значення має бути скінченним числом."], result.Errors[nameof(UpsertThresholdDto.MinValue)]);
            Assert.Equal(["Максимальне значення має бути скінченним числом."], result.Errors[nameof(UpsertThresholdDto.MaxValue)]);
        }
    }
}
