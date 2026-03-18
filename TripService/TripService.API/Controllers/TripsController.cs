using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripService.Application.DTOs.Trip;
using TripService.Application.DTOs.TripLog;
using TripService.Application.Services;

namespace TripService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TripsController : ControllerBase
{
    private readonly TripAppService _tripService;

    public TripsController(TripAppService tripService)
    {
        _tripService = tripService;
    }

    // GET api/trips/{id}
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,FleetManager,Dispatcher,Driver")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var trip = await _tripService.GetByIdAsync(id);
        return Ok(trip);
    }

    // GET api/trips/filter?status=InProgress&driverId=...&from=...&to=...
    [HttpGet("filter")]
    [Authorize(Roles = "Admin,FleetManager,Dispatcher,Driver")]
    public async Task<IActionResult> GetByFilter([FromQuery] TripFilterDto filter)
    {
        var trips = await _tripService.GetByFilterAsync(filter);
        return Ok(trips);
    }

    // POST api/trips
    [HttpPost]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> Create([FromBody] CreateTripDto dto)
    {
        var trip = await _tripService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = trip.Id }, trip);
    }

    // PUT api/trips/{id}/assign
    [HttpPut("{id:guid}/assign")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> AssignResources(Guid id, [FromBody] AssignTripDto dto)
    {
        var trip = await _tripService.AssignResourcesAsync(id, dto);
        return Ok(trip);
    }

    // PUT api/trips/{id}/start
    [HttpPut("{id:guid}/start")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> Start(Guid id)
    {
        var trip = await _tripService.StartAsync(id);
        return Ok(trip);
    }

    // PUT api/trips/{id}/complete
    [HttpPut("{id:guid}/complete")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> Complete(Guid id)
    {
        var trip = await _tripService.CompleteAsync(id);
        return Ok(trip);
    }

    // POST api/trips/{id}/logs
    [HttpPost("{id:guid}/logs")]
    [Authorize(Roles = "Admin,Driver")]
    public async Task<IActionResult> AddLog(Guid id, [FromBody] AddTripLogDto dto)
    {
        var log = await _tripService.AddLogAsync(id, dto);
        return Ok(log);
    }
}