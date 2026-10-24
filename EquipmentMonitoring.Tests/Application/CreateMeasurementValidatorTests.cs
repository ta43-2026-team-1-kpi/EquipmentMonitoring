using EquipmentMonitoring.Application.DTOs.Measurement;
using EquipmentMonitoring.Application.Validation;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Tests.Application
{
    public class CreateMeasurementValidatorTests
    {
        [Fact]
        public void Validate_WhenDtoIsValidForTemperature_ShouldBeValid()
        {
            var dto = CreateValidDto();

            var validator = new CreateMeasurementValidator();

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WhenTypeMissing_ShouldReturnTypeError()
        {
            var dto = CreateValidDto();
            dto.Type = null;

            var validator = new CreateMeasurementValidator();

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(nameof(CreateMeasurementDto.Type), result.Errors.Keys);
        }

        [Fact]
        public void Validate_WhenValueMissing_ShouldReturnValueError()
        {
            var dto = CreateValidDto();
            dto.Value = null;

            var validator = new CreateMeasurementValidator();

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(nameof(CreateMeasurementDto.Value), result.Errors.Keys);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void Validate_WhenValueIsNotFinite_ShouldReturnValueError(double invalidValue)
        {
            var dto = CreateValidDto();
            dto.Value = invalidValue;

            var validator = new CreateMeasurementValidator();

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(nameof(CreateMeasurementDto.Value), result.Errors.Keys);
        }

        [Fact]
        public void Validate_WhenValueIsZero_ShouldBeValid()
        {
            var dto = CreateValidDto();
            dto.Value = 0;

            var validator = new CreateMeasurementValidator();

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WhenTemperatureIsNegative_ShouldBeValid()
        {
            var dto = CreateValidDto();
            dto.Type = MeasurementType.Temperature;
            dto.Unit = "°C";
            dto.Value = -12.5;

            var validator = new CreateMeasurementValidator();

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData(MeasurementType.Temperature, "V")]
        [InlineData(MeasurementType.Voltage, "%")]
        [InlineData(MeasurementType.Load, "°C")]
        public void Validate_WhenUnitDoesNotMatchType_ShouldReturnUnitError(
            MeasurementType type,
            string unit)
        {
            var dto = CreateValidDto();
            dto.Type = type;
            dto.Unit = unit;

            var validator = new CreateMeasurementValidator();

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(nameof(CreateMeasurementDto.Unit), result.Errors.Keys);
        }

        [Fact]
        public void Validate_WhenMeasuredAtMissing_ShouldReturnMeasuredAtError()
        {
            var dto = CreateValidDto();
            dto.MeasuredAt = null;

            var validator = new CreateMeasurementValidator();

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(nameof(CreateMeasurementDto.MeasuredAt), result.Errors.Keys);
        }

        [Fact]
        public void Validate_WhenMeasuredAtOffsetIsZero_ShouldBeValid()
        {
            var dto = CreateValidDto();
            dto.MeasuredAt = new DateTimeOffset(2026, 10, 24, 10, 30, 0, TimeSpan.Zero);

            var validator = new CreateMeasurementValidator();

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WhenMeasuredAtHasLocalOffset_ShouldBeValid()
        {
            var dto = CreateValidDto();
            dto.MeasuredAt = new DateTimeOffset(2026, 10, 24, 12, 30, 0, TimeSpan.FromHours(2));

            var validator = new CreateMeasurementValidator();

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WhenEnumOutOfRange_ShouldReturnTypeError()
        {
            var dto = CreateValidDto();
            dto.Type = (MeasurementType)999;

            var validator = new CreateMeasurementValidator();

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(nameof(CreateMeasurementDto.Type), result.Errors.Keys);
        }

        private static CreateMeasurementDto CreateValidDto()
        {
            return new CreateMeasurementDto
            {
                Type = MeasurementType.Temperature,
                Value = 23.7,
                Unit = "°C",
                MeasuredAt = new DateTimeOffset(2026, 10, 24, 10, 30, 0, TimeSpan.Zero)
            };
        }
    }
}
