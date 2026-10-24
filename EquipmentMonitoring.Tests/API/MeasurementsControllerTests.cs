using EquipmentMonitoring.API.Controllers;
using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.DTOs.Measurement;
using EquipmentMonitoring.Application.Validation;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentMonitoring.Tests.API
{
    public class MeasurementsControllerTests
    {
        [Fact]
        public async Task Post_WithValidDto_ShouldReturn201AndPersistMeasurement()
        {
            var equipmentId = Guid.NewGuid();
            var equipmentRepository = new FakeEquipmentRepository { ExistsByIdResult = true };
            var measurementRepository = new FakeMeasurementRepository();
            var controller = CreateController(equipmentRepository, measurementRepository);
            var dto = CreateValidDto();

            var result = await controller.CreateAsync(equipmentId, dto, CancellationToken.None);

            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

            var response = Assert.IsType<MeasurementDto>(objectResult.Value);
            Assert.Equal(equipmentId, response.EquipmentId);
            Assert.Equal(dto.Type, response.Type);
            Assert.Equal(dto.Value, response.Value);
            Assert.Equal(dto.Unit, response.Unit);
            Assert.Equal(dto.MeasuredAt!.Value.UtcDateTime, response.MeasuredAtUtc.UtcDateTime);

            Assert.Equal(1, measurementRepository.AddCallsCount);
            Assert.Equal(1, measurementRepository.SaveChangesCallsCount);
        }

        [Fact]
        public async Task Post_WhenEquipmentDoesNotExist_ShouldReturn404()
        {
            var equipmentRepository = new FakeEquipmentRepository { ExistsByIdResult = false };
            var measurementRepository = new FakeMeasurementRepository();
            var controller = CreateController(equipmentRepository, measurementRepository);

            var result = await controller.CreateAsync(Guid.NewGuid(), CreateValidDto(), CancellationToken.None);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
            Assert.Equal(0, measurementRepository.AddCallsCount);
        }

        [Fact]
        public async Task Get_WhenEquipmentDoesNotExist_ShouldReturn404()
        {
            var equipmentRepository = new FakeEquipmentRepository { ExistsByIdResult = false };
            var measurementRepository = new FakeMeasurementRepository();
            var controller = CreateController(equipmentRepository, measurementRepository);

            var result = await controller.GetHistoryAsync(Guid.NewGuid(), null, null, null, CancellationToken.None);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
            Assert.Equal(0, measurementRepository.GetHistoryCallsCount);
        }

        [Fact]
        public async Task Get_WhenFromGreaterThanTo_ShouldReturn400()
        {
            var equipmentRepository = new FakeEquipmentRepository { ExistsByIdResult = true };
            var measurementRepository = new FakeMeasurementRepository();
            var controller = CreateController(equipmentRepository, measurementRepository);

            var from = "2026-10-24T12:00:00Z";
            var to = "2026-10-24T11:00:00Z";

            var result = await controller.GetHistoryAsync(Guid.NewGuid(), null, from, to, CancellationToken.None);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
            Assert.Equal(0, measurementRepository.GetHistoryCallsCount);
        }

        [Fact]
        public async Task Get_WhenEquipmentExistsWithoutMeasurements_ShouldReturn200WithEmptyList()
        {
            var equipmentRepository = new FakeEquipmentRepository { ExistsByIdResult = true };
            var measurementRepository = new FakeMeasurementRepository { GetHistoryResult = [] };
            var controller = CreateController(equipmentRepository, measurementRepository);

            var result = await controller.GetHistoryAsync(Guid.NewGuid(), null, null, null, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
            var response = Assert.IsAssignableFrom<IReadOnlyList<MeasurementDto>>(ok.Value);
            Assert.Empty(response);
        }

        private static MeasurementsController CreateController(
            FakeEquipmentRepository equipmentRepository,
            FakeMeasurementRepository measurementRepository)
        {
            var registrationValidator = new MeasurementRegistrationValidator(
                new CreateMeasurementValidator(),
                equipmentRepository);

            return new MeasurementsController(
                equipmentRepository,
                measurementRepository,
                registrationValidator)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        private static CreateMeasurementDto CreateValidDto()
        {
            return new CreateMeasurementDto
            {
                Type = MeasurementType.Temperature,
                Value = 18.3,
                Unit = "°C",
                MeasuredAt = new DateTimeOffset(2026, 10, 24, 10, 30, 0, TimeSpan.Zero)
            };
        }

        private sealed class FakeEquipmentRepository : IEquipmentRepository
        {
            public bool ExistsByIdResult { get; set; }

            public Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task<IReadOnlyList<Equipment>> GetAllAsync(EquipmentStatus? status = null, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<Equipment>>([]);
            }

            public Task<bool> ExistsBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(false);
            }

            public Task<bool> ExistsByIdAsync(Guid equipmentId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(ExistsByIdResult);
            }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(0);
            }
        }

        private sealed class FakeMeasurementRepository : IMeasurementRepository
        {
            public int AddCallsCount { get; private set; }

            public int SaveChangesCallsCount { get; private set; }

            public int GetHistoryCallsCount { get; private set; }

            public IReadOnlyList<Measurement> GetHistoryResult { get; set; } = [];

            public Task AddAsync(Measurement measurement, CancellationToken cancellationToken = default)
            {
                AddCallsCount++;
                return Task.CompletedTask;
            }

            public Task<IReadOnlyList<Measurement>> GetByEquipmentAsync(
                Guid equipmentId,
                MeasurementType? type,
                DateTime? fromUtc,
                DateTime? toUtc,
                CancellationToken cancellationToken = default)
            {
                GetHistoryCallsCount++;
                return Task.FromResult(GetHistoryResult);
            }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                SaveChangesCallsCount++;
                return Task.FromResult(1);
            }
        }
    }
}
