using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.DTOs.Equipment;
using EquipmentMonitoring.Application.Validation;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;

namespace EquipmentMonitoring.Tests.Application
{
    public class EquipmentRegistrationValidatorTests
    {
        [Fact]
        public async Task ValidateAsync_WhenSerialNumberAlreadyExists_ShouldReturnSerialNumberError()
        {
            var dto = CreateValidDto();
            var fakeRepository = new FakeEquipmentRepository
            {
                ExistsBySerialNumberResult = true
            };
            var validator = new EquipmentRegistrationValidator(new CreateEquipmentValidator(), fakeRepository);

            var result = await validator.ValidateAsync(dto);

            Assert.False(result.IsValid);
            Assert.True(result.Errors.ContainsKey(nameof(CreateEquipmentDto.SerialNumber)));
            Assert.Contains("Обладнання з таким серійним номером уже існує.", result.Errors[nameof(CreateEquipmentDto.SerialNumber)]);
            Assert.Equal(1, fakeRepository.ExistsBySerialNumberCallsCount);
            Assert.Equal(dto.SerialNumber, fakeRepository.LastRequestedSerialNumber);
        }

        [Fact]
        public async Task ValidateAsync_WhenSerialNumberIsAvailable_ShouldReturnValidResult()
        {
            var dto = CreateValidDto();
            var fakeRepository = new FakeEquipmentRepository
            {
                ExistsBySerialNumberResult = false
            };
            var validator = new EquipmentRegistrationValidator(new CreateEquipmentValidator(), fakeRepository);

            var result = await validator.ValidateAsync(dto);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
            Assert.Equal(1, fakeRepository.ExistsBySerialNumberCallsCount);
            Assert.Equal(dto.SerialNumber, fakeRepository.LastRequestedSerialNumber);
        }

        [Fact]
        public async Task ValidateAsync_WhenDtoIsInvalid_ShouldReturnFieldErrorsAndNotCallRepository()
        {
            var dto = CreateValidDto();
            dto.Name = null;
            dto.Status = null;

            var fakeRepository = new FakeEquipmentRepository();
            var validator = new EquipmentRegistrationValidator(new CreateEquipmentValidator(), fakeRepository);

            var result = await validator.ValidateAsync(dto);

            Assert.False(result.IsValid);
            Assert.True(result.Errors.ContainsKey(nameof(CreateEquipmentDto.Name)));
            Assert.True(result.Errors.ContainsKey(nameof(CreateEquipmentDto.Status)));
            Assert.Equal(0, fakeRepository.ExistsBySerialNumberCallsCount);
            Assert.Null(fakeRepository.LastRequestedSerialNumber);
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

        private sealed class FakeEquipmentRepository : IEquipmentRepository
        {
            public bool ExistsBySerialNumberResult { get; set; }

            public int ExistsBySerialNumberCallsCount { get; private set; }

            public string? LastRequestedSerialNumber { get; private set; }

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
                ExistsBySerialNumberCallsCount++;
                LastRequestedSerialNumber = serialNumber;
                return Task.FromResult(ExistsBySerialNumberResult);
            }

            public Task<bool> ExistsByIdAsync(
                Guid equipmentId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(false);
            }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(0);
            }
        }
    }
}
