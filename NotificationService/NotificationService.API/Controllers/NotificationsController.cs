using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using System.Security.Claims;
using Shared.Exceptions;

namespace NotificationService.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service)
        => _service = service;
    

    // Get my notifications based on my role.
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications([FromQuery] bool? unreadOnly = null)
    {
        // Extract all roles from JWT
        var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        if (!roles.Any())
            return Unauthorized("No roles found in token.");

        // Collect notifications for all user's roles
        var notifications = new List<NotificationDto>();
        
        foreach (var role in roles)
        {
            // Only get notifications for fleet management roles
            if (new[] { "Admin", "FleetManager", "Dispatcher", "Driver" }.Contains(role))
            {
                var roleNotifications = await _service.GetByRoleAsync(role, unreadOnly);
                notifications.AddRange(roleNotifications);
            }
        }

        // Remove duplicates and return sorted by most recent
        return Ok(notifications
            .DistinctBy(n => n.Id)
            .OrderByDescending(n => n.CreatedAt));
    }
    

    // Get all notifications for a specific role.
    [HttpGet("role/{role}")]
    [Authorize(Roles = "Admin,FleetManager")]
    public async Task<IActionResult> GetByRole(
        [FromRoute] string role,
        [FromQuery] bool? unreadOnly = null)
    {
        // Validate role is one we support
        if (!new[] { "Admin", "FleetManager", "Dispatcher", "Driver" }.Contains(role))
            return BadRequest($"Invalid role: {role}");

        var result = await _service.GetByRoleAsync(role, unreadOnly);
        return Ok(result);
    }
    
    [HttpGet("driver/{driverId:guid}")]
    [Authorize(Roles = "Admin,FleetManager")]
    public async Task<IActionResult> GetByDriverId(
        [FromRoute] Guid driverId,
        [FromQuery] bool? unreadOnly = null)
    {
        var result = await _service.GetByDriverIdAsync(driverId, unreadOnly);
        return Ok(result);
    }

    // Users can only mark notifications for roles they have except admin
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead([FromRoute] Guid id)
    {
        var notification = await _service.GetByIdAsync(id);
        
        if (notification == null)
            throw new NotFoundException($"Notification {id} not found.");

        // Get user's roles from JWT
        var userRoles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .Where(r => new[] { "Admin", "FleetManager", "Dispatcher", "Driver" }.Contains(r))
            .ToList();

        // Check authorization
        bool isAuthorized = userRoles.Contains("Admin") || 
                            userRoles.Contains(notification.TargetRole);

        if (!isAuthorized)
        {
            throw new ForbiddenException(
                $"You cannot mark this notification as read. " +
                $"This notification is for role '{notification.TargetRole}' " +
                $"but you have roles: {string.Join(", ", userRoles)}");
        }

        await _service.MarkAsReadAsync(id);
        return NoContent();
    }
}