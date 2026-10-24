using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.DTOs.Measurement;
using EquipmentMonitoring.Application.Validation;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Tests.Application
{
    public class MeasurementRegistrationValidatorTests
    {
        [Fact]
        public async Task ValidateAsync_WhenEquipmentDoesNotExist_ShouldReturnEquipmentIdError()
        {
            var equipmentRepository = new FakeEquipmentRepository
            {
                ExistsByIdResult = false
            };
            var validator = new MeasurementRegistrationValidator(
                new CreateMeasurementValidator(),
                equipmentRepository);

            var result = await validator.ValidateAsync(Guid.NewGuid(), CreateValidDto());

            Assert.False(result.IsValid);
            Assert.Contains("equipmentId", result.Errors.Keys);
            Assert.Equal(1, equipmentRepository.ExistsByIdCallsCount);
        }

        [Fact]
        public async Task ValidateAsync_WhenEquipmentExistsAndDtoValid_ShouldBeValid()
        {
            var equipmentRepository = new FakeEquipmentRepository
            {
                ExistsByIdResult = true
            };
            var validator = new MeasurementRegistrationValidator(
                new CreateMeasurementValidator(),
                equipmentRepository);

            var result = await validator.ValidateAsync(Guid.NewGuid(), CreateValidDto());

            Assert.True(result.IsValid);
            Assert.Equal(1, equipmentRepository.ExistsByIdCallsCount);
        }

        [Fact]
        public async Task ValidateAsync_WhenDtoInvalid_ShouldNotCallEquipmentRepository()
        {
            var equipmentRepository = new FakeEquipmentRepository
            {
                ExistsByIdResult = true
            };
            var validator = new MeasurementRegistrationValidator(
                new CreateMeasurementValidator(),
                equipmentRepository);

            var dto = CreateValidDto();
            dto.Unit = null;

            var result = await validator.ValidateAsync(Guid.NewGuid(), dto);

            Assert.False(result.IsValid);
            Assert.Equal(0, equipmentRepository.ExistsByIdCallsCount);
        }

        private static CreateMeasurementDto CreateValidDto()
        {
            return new CreateMeasurementDto
            {
                Type = MeasurementType.Voltage,
                Value = 220,
                Unit = "V",
                MeasuredAt = new DateTimeOffset(2026, 10, 24, 10, 30, 0, TimeSpan.Zero)
            };
        }

        private sealed class FakeEquipmentRepository : IEquipmentRepository
        {
            public bool ExistsByIdResult { get; set; }

            public int ExistsByIdCallsCount { get; private set; }

            public Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task<IReadOnlyList<Equipment>> GetAllAsync(
                EquipmentStatus? status = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<Equipment>>([]);
            }

            public Task<bool> ExistsBySerialNumberAsync(
                string serialNumber,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(false);
            }

            public Task<bool> ExistsByIdAsync(Guid equipmentId, CancellationToken cancellationToken = default)
            {
                ExistsByIdCallsCount++;
                return Task.FromResult(ExistsByIdResult);
            }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(0);
            }
        }
    }
}
