using FleetService.API.Controllers;
using FleetService.Application.DTOs.Vehicle;
using FleetService.Application.Interfaces;
using FleetService.Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shared.Exceptions;
using Xunit;

namespace FleetService.Tests.API;

public class VehiclesControllerTests
{
    private readonly Mock<IVehicleService> _serviceMock;
    private readonly VehiclesController  _controller;

    public VehiclesControllerTests()
    {
        // Mock the service — controller delegates to it
        _serviceMock = new Mock<IVehicleService>();
        _controller  = new VehiclesController(_serviceMock.Object);
    }

    [Fact]
    public async Task GetById_WhenFound_ShouldReturn200()
    {
        var dto = new VehicleDto { Id = Guid.NewGuid(), PlateNumber = "ABC-1234" };

        _serviceMock.Setup(s => s.GetByIdAsync(dto.Id))
                    .ReturnsAsync(dto);

        var result = await _controller.GetById(dto.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturn404()
    {
        var id = Guid.NewGuid();

        _serviceMock.Setup(s => s.GetByIdAsync(id))
                    .ThrowsAsync(new NotFoundException($"Vehicle {id} not found."));

        // The middleware handles this in real app — in unit test we catch the exception
        Func<Task> act = () => _controller.GetById(id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Create_WhenValid_ShouldReturn201()
    {
        var dto    = new CreateVehicleDto { PlateNumber = "ABC-1234", Model = "Toyota", Year = 2022, Type = "Sedan" };
        var result = new VehicleDto { Id = Guid.NewGuid(), PlateNumber = "ABC-1234" };

        _serviceMock.Setup(s => s.CreateAsync(dto)).ReturnsAsync(result);

        var response = await _controller.Create(dto);

        var created = response.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Delete_WhenFound_ShouldReturn204()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id)).Returns(Task.CompletedTask);

        var response = await _controller.Delete(id);

        response.Should().BeOfType<NoContentResult>()
                .Which.StatusCode.Should().Be(204);
    }
}