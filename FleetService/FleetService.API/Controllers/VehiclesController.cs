using FleetService.Application.DTOs.Vehicle;
using FleetService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FleetService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly VehicleService _vehicleService;

    public VehiclesController(VehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    // GET api/vehicles
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var vehicles = await _vehicleService.GetAllAsync();
        return Ok(vehicles);
    }

    // GET api/vehicles/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var vehicle = await _vehicleService.GetByIdAsync(id);
        return Ok(vehicle);
    }

    // GET api/vehicles/filter?status=Active&type=Sedan
    [HttpGet("filter")]
    public async Task<IActionResult> GetByFilter([FromQuery] string? status, [FromQuery] string? type)
    {
        var vehicles = await _vehicleService.GetByFilterAsync(status, type);
        return Ok(vehicles);
    }

    // POST api/vehicles
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleDto dto)
    {
        var vehicle = await _vehicleService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, vehicle);
    }

    // PUT api/vehicles/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleDto dto)
    {
        var vehicle = await _vehicleService.UpdateAsync(id, dto);
        return Ok(vehicle);
    }

    // DELETE api/vehicles/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _vehicleService.DeleteAsync(id);
        return NoContent();
    }

    // State transitions

    // PUT api/vehicles/{id}/activate
    [HttpPut("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var vehicle = await _vehicleService.ActivateAsync(id);
        return Ok(vehicle);
    }

    // PUT api/vehicles/{id}/maintenance/start
    [HttpPut("{id:guid}/maintenance/start")]
    public async Task<IActionResult> SendToMaintenance(Guid id)
    {
        var vehicle = await _vehicleService.SendToMaintenanceAsync(id);
        return Ok(vehicle);
    }

    // PUT api/vehicles/{id}/maintenance/complete
    [HttpPut("{id:guid}/maintenance/complete")]
    public async Task<IActionResult> CompleteMaintenance(Guid id)
    {
        var vehicle = await _vehicleService.CompleteMaintenanceAsync(id);
        return Ok(vehicle);
    }

    // PUT api/vehicles/{id}/decommission
    [HttpPut("{id:guid}/decommission")]
    public async Task<IActionResult> Decommission(Guid id)
    {
        var vehicle = await _vehicleService.DecommissionAsync(id);
        return Ok(vehicle);
    }

    // Assignment

    // PUT api/vehicles/{id}/assign/{driverId}
    [HttpPut("{id:guid}/assign/{driverId:guid}")]
    public async Task<IActionResult> AssignDriver(Guid id, Guid driverId)
    {
        var vehicle = await _vehicleService.AssignDriverAsync(id, driverId);
        return Ok(vehicle);
    }

    // PUT api/vehicles/{id}/unassign
    [HttpPut("{id:guid}/unassign")]
    public async Task<IActionResult> UnassignDriver(Guid id)
    {
        var vehicle = await _vehicleService.UnassignDriverAsync(id);
        return Ok(vehicle);
    }
}