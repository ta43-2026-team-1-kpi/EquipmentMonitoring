using EquipmentMonitoring.Application.DTOs.Maintenance;
using EquipmentMonitoring.Application.Validation;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Tests.Application
{
    public class UpdateMaintenancePlanStatusValidatorTests
    {
        private readonly UpdateMaintenancePlanStatusValidator _validator = new();

        [Theory]
        [InlineData(MaintenancePlanStatus.Completed)]
        [InlineData(MaintenancePlanStatus.Cancelled)]
        public void Validate_WithAllowedTargetStatus_ShouldReturnValidResult(
            MaintenancePlanStatus status)
        {
            var result = _validator.Validate(new UpdateMaintenancePlanStatusDto
            {
                Status = status
            });

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(MaintenancePlanStatus.Planned)]
        public void Validate_WithMissingOrNonTerminalStatus_ShouldReturnError(
            MaintenancePlanStatus? status)
        {
            var result = _validator.Validate(new UpdateMaintenancePlanStatusDto
            {
                Status = status
            });

            Assert.False(result.IsValid);
            Assert.Contains(nameof(UpdateMaintenancePlanStatusDto.Status), result.Errors.Keys);
        }

        [Fact]
        public void Validate_WithUndefinedEnumValue_ShouldReturnError()
        {
            var result = _validator.Validate(new UpdateMaintenancePlanStatusDto
            {
                Status = (MaintenancePlanStatus)999
            });

            Assert.False(result.IsValid);
            Assert.Contains(nameof(UpdateMaintenancePlanStatusDto.Status), result.Errors.Keys);
        }
    }
}
