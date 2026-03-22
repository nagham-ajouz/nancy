using AutoMapper;
using FleetService.Application.DTOs.Vehicle;
using FleetService.Application.Interfaces;
using FleetService.Application.Mapping;
using FleetService.Application.Services;
using FleetService.Domain.Entities;
using FleetService.Domain.Enums;
using FleetService.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Shared.BaseClasses;
using Shared.Exceptions;
using Xunit;

namespace FleetService.Tests.Application;

public class VehicleServiceTests
{
    // All dependencies mocked — no real DB or RabbitMQ
    private readonly Mock<IVehicleRepository>   _vehicleRepoMock   = new();
    private readonly Mock<IDriverRepository>    _driverRepoMock    = new();
    private readonly Mock<IFleetCacheService>   _cacheMock         = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock   = new();
    
    private readonly IMapper _mapper;
    private readonly VehicleService _service;

    public VehicleServiceTests()
    {
        // Real AutoMapper with actual profile
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<FleetMappingProfile>());
        _mapper = config.CreateMapper();

        _service = new VehicleService(
            _vehicleRepoMock.Object,
            _driverRepoMock.Object, 
            _dispatcherMock.Object,
            _cacheMock.Object,
            _mapper
           );
    }

    [Fact]
    public async Task GetByIdAsync_WhenVehicleExists_ShouldReturnDto()
    {
        var vehicle = new Vehicle(Guid.NewGuid(), new PlateNumber("ABC-1234"),
                                  "Toyota", 2022, VehicleType.Sedan);

        // Mock returns the vehicle when asked for its ID
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicle.Id))
                        .ReturnsAsync(vehicle);

        var result = await _service.GetByIdAsync(vehicle.Id);

        result.Should().NotBeNull();
        result.PlateNumber.Should().Be("ABC-1234");
        result.Model.Should().Be("Toyota");
    }

    [Fact]
    public async Task GetByIdAsync_WhenVehicleNotFound_ShouldThrowNotFoundException()
    {
        var nonExistentId = Guid.NewGuid();

        // Mock returns null — vehicle doesn't exist
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(nonExistentId))
                        .ReturnsAsync((Vehicle?)null);

        Func<Task> act = () => _service.GetByIdAsync(nonExistentId);

        await act.Should().ThrowAsync<NotFoundException>()
                 .WithMessage($"*{nonExistentId}*");
    }

    [Fact]
    public async Task CreateAsync_ShouldSaveAndReturnDto()
    {
        var dto = new CreateVehicleDto
        {
            PlateNumber = "ABC-1234",
            Model       = "Toyota",
            Year        = 2022,
            Type        = "Sedan"
        };

        // Mock: AddAsync does nothing (void), cache invalidation does nothing
        _vehicleRepoMock.Setup(r => r.AddAsync(It.IsAny<Vehicle>()))
                        .Returns(Task.CompletedTask);
        _cacheMock.Setup(c => c.InvalidateVehiclesAsync())
                  .Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(dto);

        result.PlateNumber.Should().Be("ABC-1234");
        result.Status.Should().Be("Registered");

        // Verify AddAsync was actually called once
        _vehicleRepoMock.Verify(r => r.AddAsync(It.IsAny<Vehicle>()), Times.Once);
        // Verify cache was invalidated
        _cacheMock.Verify(c => c.InvalidateVehiclesAsync(), Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_WhenVehicleFound_ShouldActivateAndPublish()
    {
        var vehicle = new Vehicle(Guid.NewGuid(), new PlateNumber("ABC-1234"),
                                  "Toyota", 2022, VehicleType.Sedan);

        _vehicleRepoMock.Setup(r => r.GetByIdAsync(vehicle.Id))
                        .ReturnsAsync(vehicle);
        _vehicleRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Vehicle>()))
                        .Returns(Task.CompletedTask);
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<Entity>()))
                       .Returns(Task.CompletedTask);
        _cacheMock.Setup(c => c.InvalidateVehiclesAsync())
                  .Returns(Task.CompletedTask);

        var result = await _service.ActivateAsync(vehicle.Id);

        result.Status.Should().Be("Active");

        // Verify dispatcher was called — it handles publishing
        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<Entity>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenVehicleNotFound_ShouldThrow()
    {
        _vehicleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                        .ReturnsAsync((Vehicle?)null);

        Func<Task> act = () => _service.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}