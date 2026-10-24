using EquipmentMonitoring.Application.DTOs.Equipment;
using EquipmentMonitoring.Application.Validation;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Tests.Application
{
    public class CreateEquipmentValidatorTests
    {
        [Fact]
        public void Validate_WhenStatusIsMissing_ShouldReturnStatusError()
        {
            var dto = new CreateEquipmentDto
            {
                Name = "Compressor A1",
                Type = "Compressor",
                SerialNumber = "SN-2026-0001",
                Location = "Workshop 1",
                Status = null
            };

            var validator = new CreateEquipmentValidator();

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.True(result.Errors.ContainsKey("Status"));
            Assert.Contains("Вкажіть стан обладнання.", result.Errors["Status"]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_WhenNameIsNullEmptyOrWhitespace_ShouldReturnNameError(string? name)
        {
            var dto = CreateValidDto();
            dto.Name = name;

            var validator = new CreateEquipmentValidator();

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.True(result.Errors.ContainsKey("Name"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_WhenTypeIsNullEmptyOrWhitespace_ShouldReturnTypeError(string? type)
        {
            var dto = CreateValidDto();
            dto.Type = type;

            var validator = new CreateEquipmentValidator();

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.True(result.Errors.ContainsKey("Type"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_WhenSerialNumberIsNullEmptyOrWhitespace_ShouldReturnSerialNumberError(string? serialNumber)
        {
            var dto = CreateValidDto();
            dto.SerialNumber = serialNumber;

            var validator = new CreateEquipmentValidator();

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.True(result.Errors.ContainsKey("SerialNumber"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_WhenLocationIsNullEmptyOrWhitespace_ShouldReturnLocationError(string? location)
        {
            var dto = CreateValidDto();
            dto.Location = location;

            var validator = new CreateEquipmentValidator();

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.True(result.Errors.ContainsKey("Location"));
        }

        [Fact]
        public void Validate_WhenNameLengthExceeds200_ShouldReturnNameError()
        {
            var dto = CreateValidDto();
            dto.Name = new string('N', 201);

            var validator = new CreateEquipmentValidator();

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.True(result.Errors.ContainsKey("Name"));
        }

        [Fact]
        public void Validate_WhenTypeLengthExceeds100_ShouldReturnTypeError()
        {
            var dto = CreateValidDto();
            dto.Type = new string('T', 101);

            var validator = new CreateEquipmentValidator();

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.True(result.Errors.ContainsKey("Type"));
        }

        [Fact]
        public void Validate_WhenSerialNumberLengthExceeds100_ShouldReturnSerialNumberError()
        {
            var dto = CreateValidDto();
            dto.SerialNumber = new string('S', 101);

            var validator = new CreateEquipmentValidator();

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.True(result.Errors.ContainsKey("SerialNumber"));
        }

        [Fact]
        public void Validate_WhenLocationLengthExceeds200_ShouldReturnLocationError()
        {
            var dto = CreateValidDto();
            dto.Location = new string('L', 201);

            var validator = new CreateEquipmentValidator();

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.True(result.Errors.ContainsKey("Location"));
        }

        [Fact]
        public void Validate_WhenTextFieldsAreAtMaxLength_ShouldBeValid()
        {
            var dto = CreateValidDto();
            dto.Name = new string('N', 200);
            dto.Type = new string('T', 100);
            dto.SerialNumber = new string('S', 100);
            dto.Location = new string('L', 200);

            var validator = new CreateEquipmentValidator();

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WhenStatusIsOutOfRange_ShouldReturnStatusError()
        {
            var dto = CreateValidDto();
            dto.Status = (EquipmentStatus)999;

            var validator = new CreateEquipmentValidator();

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.True(result.Errors.ContainsKey("Status"));
            Assert.Contains("Неприпустимий стан обладнання.", result.Errors["Status"]);
        }

        [Theory]
        [InlineData(EquipmentStatus.Working)]
        [InlineData(EquipmentStatus.Faulty)]
        [InlineData(EquipmentStatus.UnderMaintenance)]
        public void Validate_WhenDtoIsValidWithAllowedStatus_ShouldBeValid(EquipmentStatus status)
        {
            var dto = CreateValidDto();
            dto.Status = status;

            var validator = new CreateEquipmentValidator();

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WhenMultipleFieldsAreInvalid_ShouldContainErrorsForAllInvalidFields()
        {
            var dto = CreateValidDto();
            dto.Name = null;
            dto.SerialNumber = "   ";
            dto.Status = null;

            var validator = new CreateEquipmentValidator();

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.True(result.Errors.ContainsKey("Name"));
            Assert.True(result.Errors.ContainsKey("SerialNumber"));
            Assert.True(result.Errors.ContainsKey("Status"));
        }

        private static CreateEquipmentDto CreateValidDto()
        {
            return new CreateEquipmentDto
            {
                Name = "Compressor A1",
                Type = "Compressor",
                SerialNumber = "SN-2026-0001",
                Location = "Workshop 1",
                Status = EquipmentStatus.Working
            };
        }
    }
}
