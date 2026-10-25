using EquipmentMonitoring.Application.DTOs.Maintenance;
using EquipmentMonitoring.Application.Validation;

namespace EquipmentMonitoring.Tests.Application
{
    public class CreateMaintenancePlanValidatorTests
    {
        private readonly CreateMaintenancePlanValidator _validator = new();

        [Fact]
        public void Validate_WithValidDto_ShouldReturnValidResult()
        {
            var result = _validator.Validate(CreateValidDto());

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WhenRequiredFieldsAreMissing_ShouldReturnFieldErrors()
        {
            var result = _validator.Validate(new CreateMaintenancePlanDto
            {
                EquipmentId = Guid.Empty,
                PlannedDate = null,
                WorkType = " ",
                ResponsibleEmployee = ""
            });

            Assert.False(result.IsValid);
            Assert.Contains(nameof(CreateMaintenancePlanDto.EquipmentId), result.Errors.Keys);
            Assert.Contains(nameof(CreateMaintenancePlanDto.PlannedDate), result.Errors.Keys);
            Assert.Contains(nameof(CreateMaintenancePlanDto.WorkType), result.Errors.Keys);
            Assert.Contains(nameof(CreateMaintenancePlanDto.ResponsibleEmployee), result.Errors.Keys);
        }

        [Fact]
        public void Validate_WhenPlannedDateIsDefault_ShouldReturnError()
        {
            var dto = CreateValidDto();
            dto.PlannedDate = DateOnly.MinValue;

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(nameof(CreateMaintenancePlanDto.PlannedDate), result.Errors.Keys);
        }

        [Theory]
        [InlineData(nameof(CreateMaintenancePlanDto.WorkType), 201)]
        [InlineData(nameof(CreateMaintenancePlanDto.ResponsibleEmployee), 201)]
        [InlineData(nameof(CreateMaintenancePlanDto.Note), 1001)]
        public void Validate_WhenTextFieldExceedsLimit_ShouldReturnError(
            string fieldName,
            int length)
        {
            var dto = CreateValidDto();
            var value = new string('x', length);

            switch (fieldName)
            {
                case nameof(CreateMaintenancePlanDto.WorkType):
                    dto.WorkType = value;
                    break;
                case nameof(CreateMaintenancePlanDto.ResponsibleEmployee):
                    dto.ResponsibleEmployee = value;
                    break;
                case nameof(CreateMaintenancePlanDto.Note):
                    dto.Note = value;
                    break;
            }

            var result = _validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(fieldName, result.Errors.Keys);
        }

        private static CreateMaintenancePlanDto CreateValidDto()
        {
            return new CreateMaintenancePlanDto
            {
                EquipmentId = Guid.NewGuid(),
                PlannedDate = new DateOnly(2026, 11, 12),
                WorkType = "Annual inspection",
                ResponsibleEmployee = "Olena Kovalenko",
                Note = "Check the safety valves"
            };
        }
    }
}
