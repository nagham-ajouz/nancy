using FleetService.Application.DTOs.Driver;
using FleetService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DriversController : ControllerBase
{
    private readonly DriverService _driverService;

    public DriversController(DriverService driverService)
    {
        _driverService = driverService;
    }

    // GET api/drivers
    [HttpGet]
    [Authorize(Roles = "Admin,FleetManager,Dispatcher")]
    public async Task<IActionResult> GetAll()
    {
        var drivers = await _driverService.GetAllAsync();
        return Ok(drivers);
    }

    // GET api/drivers/{id}
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,FleetManager,Dispatcher")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var driver = await _driverService.GetByIdAsync(id);
        return Ok(driver);
    }

    // GET api/drivers/filter?status=Available
    [HttpGet("filter")]
    [Authorize(Roles = "Admin,FleetManager,Dispatcher")]
    public async Task<IActionResult> GetByFilter([FromQuery] string? status)
    {
        var drivers = await _driverService.GetByFilterAsync(status);
        return Ok(drivers);
    }

    // POST api/drivers
    [HttpPost]
    [Authorize(Roles = "Admin,FleetManager")]
    public async Task<IActionResult> Create([FromBody] CreateDriverDto dto)
    {
        var driver = await _driverService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = driver.Id }, driver);
    }

    // PUT api/drivers/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,FleetManager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDriverDto dto)
    {
        var driver = await _driverService.UpdateAsync(id, dto);
        return Ok(driver);
    }

    // DELETE api/drivers/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,FleetManager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _driverService.DeleteAsync(id);
        return NoContent();
    }
}