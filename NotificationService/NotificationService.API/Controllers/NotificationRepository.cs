using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;

namespace NotificationService.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service)
        => _service = service;

    /// <summary>
    /// Get all notifications for a role (Admin, FleetManager, Dispatcher, Driver).
    /// Accessible by Admin and FleetManager.
    /// </summary>
    [HttpGet("role/{role}")]
    [Authorize(Policy = "AdminOrFleetManager")]
    public async Task<IActionResult> GetByRole(
        [FromRoute] string role,
        [FromQuery] bool? unreadOnly = null)
    {
        var result = await _service.GetByRoleAsync(role, unreadOnly);
        return Ok(result);
    }

    /// <summary>
    /// Get notifications for the currently authenticated user (by userId claim).
    /// Any authenticated user can query their own notifications.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = "AnyRole")]
    public async Task<IActionResult> GetMine([FromQuery] bool? unreadOnly = null)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Cannot resolve user identity from token.");

        var result = await _service.GetByUserAsync(userId, unreadOnly);
        return Ok(result);
    }

    /// <summary>
    /// Get notifications for a specific user by ID.
    /// Admin only.
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetByUser(
        [FromRoute] Guid userId,
        [FromQuery] bool? unreadOnly = null)
    {
        var result = await _service.GetByUserAsync(userId, unreadOnly);
        return Ok(result);
    }

    /// <summary>
    /// Mark a notification as read.
    /// </summary>
    [HttpPatch("{id:guid}/read")]
    [Authorize(Policy = "AnyRole")]
    public async Task<IActionResult> MarkRead([FromRoute] Guid id)
    {
        await _service.MarkAsReadAsync(id);
        return NoContent();
    }
}