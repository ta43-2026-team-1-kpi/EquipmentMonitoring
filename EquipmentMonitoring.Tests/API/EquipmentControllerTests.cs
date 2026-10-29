using EquipmentMonitoring.API.Controllers;
using EquipmentMonitoring.Application.Abstractions.Persistence;
using EquipmentMonitoring.Application.DTOs.Equipment;
using EquipmentMonitoring.Application.Exceptions;
using EquipmentMonitoring.Application.Validation;
using EquipmentMonitoring.Domain.Entities;
using EquipmentMonitoring.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentMonitoring.Tests.API
{
    public class EquipmentControllerTests
    {
        [Fact]
        public async Task Post_WithValidDto_ShouldReturn201AndPersistEquipment()
        {
            var repository = new FakeEquipmentRepository
            {
                ExistsBySerialNumberResult = false,
                SaveChangesResult = 1
            };
            var controller = CreateController(repository);

            var dto = CreateValidDto();

            var result = await controller.CreateAsync(dto, CancellationToken.None);

            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

            var responseDto = Assert.IsType<EquipmentDto>(objectResult.Value);
            Assert.NotEqual(Guid.Empty, responseDto.Id);
            Assert.Equal(dto.Name, responseDto.Name);
            Assert.Equal(dto.Type, responseDto.Type);
            Assert.Equal(dto.SerialNumber, responseDto.SerialNumber);
            Assert.Equal(dto.Location, responseDto.Location);
            Assert.Equal(dto.Status, responseDto.Status);

            Assert.Equal(1, repository.AddCallsCount);
            Assert.Equal(1, repository.SaveChangesCallsCount);
            Assert.NotNull(repository.LastAddedEquipment);
            Assert.Equal(dto.SerialNumber, repository.LastAddedEquipment!.SerialNumber);
        }

        [Fact]
        public async Task Post_WithSerialNumberOuterSpaces_ShouldReturn201AndPersistNormalizedSerialNumber()
        {
            var repository = new FakeEquipmentRepository
            {
                ExistsBySerialNumberResult = false,
                SaveChangesResult = 1
            };
            var controller = CreateController(repository);

            var dto = CreateValidDto();
            dto.SerialNumber = "  SN-TDD-001  ";

            var result = await controller.CreateAsync(dto, CancellationToken.None);

            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

            var responseDto = Assert.IsType<EquipmentDto>(objectResult.Value);
            Assert.Equal("SN-TDD-001", responseDto.SerialNumber);

            Assert.Equal("SN-TDD-001", repository.LastExistsBySerialNumberArgument);
            Assert.NotNull(repository.LastAddedEquipment);
            Assert.Equal("SN-TDD-001", repository.LastAddedEquipment!.SerialNumber);
        }

        [Fact]
        public async Task Post_WithInvalidRequiredFields_ShouldReturn400AndNotPersist()
        {
            var repository = new FakeEquipmentRepository();
            var controller = CreateController(repository);

            var dto = new CreateEquipmentDto
            {
                Name = "",
                Type = "   ",
                SerialNumber = null,
                Location = "",
                Status = null
            };

            var result = await controller.CreateAsync(dto, CancellationToken.None);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

            var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
            Assert.Contains(nameof(CreateEquipmentDto.Name), problem.Errors.Keys);
            Assert.Contains(nameof(CreateEquipmentDto.Type), problem.Errors.Keys);
            Assert.Contains(nameof(CreateEquipmentDto.SerialNumber), problem.Errors.Keys);
            Assert.Contains(nameof(CreateEquipmentDto.Location), problem.Errors.Keys);
            Assert.Contains(nameof(CreateEquipmentDto.Status), problem.Errors.Keys);

            Assert.Equal(0, repository.AddCallsCount);
            Assert.Equal(0, repository.SaveChangesCallsCount);
            Assert.Equal(0, repository.ExistsBySerialNumberCallsCount);
        }

        [Fact]
        public async Task Post_WhenSerialNumberAlreadyExists_ShouldReturn400AndNotPersist()
        {
            var repository = new FakeEquipmentRepository
            {
                ExistsBySerialNumberResult = true
            };
            var controller = CreateController(repository);

            var dto = CreateValidDto();

            var result = await controller.CreateAsync(dto, CancellationToken.None);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

            var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
            Assert.Contains(nameof(CreateEquipmentDto.SerialNumber), problem.Errors.Keys);

            Assert.Equal(0, repository.AddCallsCount);
            Assert.Equal(0, repository.SaveChangesCallsCount);
            Assert.Equal(1, repository.ExistsBySerialNumberCallsCount);
            Assert.Equal(dto.SerialNumber, repository.LastExistsBySerialNumberArgument);
        }

        [Fact]
        public async Task Post_WhenNormalizedSerialAlreadyExists_ShouldReturn400AndNotPersist()
        {
            var repository = new FakeEquipmentRepository
            {
                ExistsBySerialNumberResult = true
            };
            var controller = CreateController(repository);

            var dto = CreateValidDto();
            dto.SerialNumber = "  SN-TDD-001  ";

            var result = await controller.CreateAsync(dto, CancellationToken.None);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

            var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
            Assert.Contains(nameof(CreateEquipmentDto.SerialNumber), problem.Errors.Keys);

            Assert.Equal(0, repository.AddCallsCount);
            Assert.Equal(0, repository.SaveChangesCallsCount);
            Assert.Equal(1, repository.ExistsBySerialNumberCallsCount);
            Assert.Equal("SN-TDD-001", repository.LastExistsBySerialNumberArgument);
        }

        [Fact]
        public async Task Post_WhenSaveChangesThrowsDuplicateException_ShouldReturn409()
        {
            var repository = new FakeEquipmentRepository
            {
                ExistsBySerialNumberResult = false,
                SaveChangesException = new DuplicateEquipmentSerialNumberException("SN-2026-0001")
            };
            var controller = CreateController(repository);

            var dto = CreateValidDto();

            var result = await controller.CreateAsync(dto, CancellationToken.None);

            var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);

            var problem = Assert.IsType<ValidationProblemDetails>(conflict.Value);
            Assert.Contains(nameof(CreateEquipmentDto.SerialNumber), problem.Errors.Keys);

            Assert.Equal(1, repository.AddCallsCount);
            Assert.Equal(1, repository.SaveChangesCallsCount);
        }

        [Fact]
        public async Task Get_WithoutStatus_ShouldReturn200AndPassNullFilter()
        {
            var repository = new FakeEquipmentRepository
            {
                GetAllResult =
                [
                    new Equipment("Compressor A1", "Compressor", "SN-001", "Workshop 1", EquipmentStatus.Working),
                    new Equipment("Pump B1", "Pump", "SN-002", "Workshop 2", EquipmentStatus.Faulty)
                ]
            };
            var controller = CreateController(repository);

            var result = await controller.GetAllAsync(null, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

            var response = Assert.IsAssignableFrom<IReadOnlyList<EquipmentDto>>(ok.Value);
            Assert.Equal(2, response.Count);
            Assert.Equal("SN-001", response[0].SerialNumber);
            Assert.Equal("SN-002", response[1].SerialNumber);
            Assert.Equal(1, repository.GetAllCallsCount);
            Assert.Null(repository.LastGetAllStatusArgument);
        }

        [Theory]
        [InlineData("working", EquipmentStatus.Working)]
        [InlineData("FAULTY", EquipmentStatus.Faulty)]
        [InlineData("uNdErMaInTeNaNcE", EquipmentStatus.UnderMaintenance)]
        public async Task Get_WithAllowedStatusNameInAnyCase_ShouldReturn200AndPassParsedFilter(
            string statusQuery,
            EquipmentStatus expectedStatus)
        {
            var repository = new FakeEquipmentRepository
            {
                GetAllResult = []
            };
            var controller = CreateController(repository);

            var result = await controller.GetAllAsync(statusQuery, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
            Assert.Equal(1, repository.GetAllCallsCount);
            Assert.Equal(expectedStatus, repository.LastGetAllStatusArgument);
        }

        [Theory]
        [InlineData("Unknown")]
        [InlineData("999")]
        [InlineData("0")]
        [InlineData("   ")]
        [InlineData("Working, Faulty")]
        public async Task Get_WithInvalidStatus_ShouldReturn400AndNotCallRepository(string invalidStatus)
        {
            var repository = new FakeEquipmentRepository();
            var controller = CreateController(repository);

            var result = await controller.GetAllAsync(invalidStatus, CancellationToken.None);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

            var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
            Assert.Contains("status", problem.Errors.Keys);
            Assert.Equal(0, repository.GetAllCallsCount);
        }

        [Fact]
        public async Task Get_WithEmptyStatusQueryParameterBoundToNull_ShouldReturn400AndNotCallRepository()
        {
            var repository = new FakeEquipmentRepository();
            var controller = CreateController(repository, "?status=");

            var result = await controller.GetAllAsync(null, CancellationToken.None);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

            var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
            Assert.Contains("status", problem.Errors.Keys);
            Assert.Equal(0, repository.GetAllCallsCount);
        }

        [Fact]
        public async Task Get_WhenNoRecords_ShouldReturn200WithEmptyList()
        {
            var repository = new FakeEquipmentRepository
            {
                GetAllResult = []
            };
            var controller = CreateController(repository);

            var result = await controller.GetAllAsync(null, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

            var response = Assert.IsAssignableFrom<IReadOnlyList<EquipmentDto>>(ok.Value);
            Assert.Empty(response);
            Assert.Equal(1, repository.GetAllCallsCount);
            Assert.Null(repository.LastGetAllStatusArgument);
        }

        private static EquipmentController CreateController(
            FakeEquipmentRepository repository,
            string? queryString = null)
        {
            var createValidator = new CreateEquipmentValidator();
            var registrationValidator = new EquipmentRegistrationValidator(createValidator, repository);
            var controller = new EquipmentController(repository, registrationValidator)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            if (queryString is not null)
            {
                controller.ControllerContext.HttpContext.Request.QueryString = new QueryString(queryString);
            }

            return controller;
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
            public int AddCallsCount { get; private set; }

            public int SaveChangesCallsCount { get; private set; }

            public int GetAllCallsCount { get; private set; }

            public int ExistsBySerialNumberCallsCount { get; private set; }

            public Equipment? LastAddedEquipment { get; private set; }

            public EquipmentStatus? LastGetAllStatusArgument { get; private set; }

            public string? LastExistsBySerialNumberArgument { get; private set; }

            public bool ExistsBySerialNumberResult { get; set; }

            public int SaveChangesResult { get; set; }

            public Exception? SaveChangesException { get; set; }

            public IReadOnlyList<Equipment> GetAllResult { get; set; } = [];

            public Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default)
            {
                AddCallsCount++;
                LastAddedEquipment = equipment;
                return Task.CompletedTask;
            }

            public Task<IReadOnlyList<Equipment>> GetAllAsync(
                EquipmentStatus? status = null,
                CancellationToken cancellationToken = default)
            {
                GetAllCallsCount++;
                LastGetAllStatusArgument = status;
                return Task.FromResult(GetAllResult);
            }

            public Task<bool> ExistsBySerialNumberAsync(
                string serialNumber,
                CancellationToken cancellationToken = default)
            {
                ExistsBySerialNumberCallsCount++;
                LastExistsBySerialNumberArgument = serialNumber;
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
                SaveChangesCallsCount++;

                if (SaveChangesException is not null)
                {
                    throw SaveChangesException;
                }

                return Task.FromResult(SaveChangesResult);
            }
        }
    }
}
