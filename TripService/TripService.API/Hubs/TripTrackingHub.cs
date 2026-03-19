using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TripService.Application.DTOs.TripLog;
using TripService.Application.Services;

namespace TripService.API.Hubs;

[Authorize]
public class TripTrackingHub : Hub
{
    private readonly TripAppService _tripService;
    private readonly ILogger<TripTrackingHub> _logger;

    public TripTrackingHub(TripAppService tripService, ILogger<TripTrackingHub> logger)
    {
        _tripService = tripService;
        _logger      = logger;
    }
    
    // ── Called by Dispatchers/Admins to subscribe to a trip ──────────────────

    // Client calls: await connection.InvokeAsync("SubscribeToTrip", tripId)
    // After this, client receives "ReceiveLocationUpdate" messages for that trip
    [Authorize(Roles = "Admin,Dispatcher,FleetManager")]
    public async Task SubscribeToTrip(Guid tripId)
    {
        // Add this connection to a SignalR group named after the trip
        await Groups.AddToGroupAsync(Context.ConnectionId, tripId.ToString());

        _logger.LogInformation(
            "Client {ConnectionId} subscribed to trip {TripId}",
            Context.ConnectionId, tripId);
    }
    
    // ── Called by Drivers to unsubscribe ────────────────────────────────────

    public async Task UnsubscribeFromTrip(Guid tripId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, tripId.ToString());

        _logger.LogInformation(
            "Client {ConnectionId} unsubscribed from trip {TripId}",
            Context.ConnectionId, tripId);
    }
    
    // ── Called by Driver to send a GPS update ───────────────────────────────

    [Authorize(Roles = "Admin,Driver")]
    public async Task SendLocationUpdate(Guid tripId, double latitude, double longitude,
        string address, decimal? speed)
    {
        _logger.LogInformation(
            "SIGNALR: Location update received for trip {TripId} | Lat: {Lat} Lng: {Lng}",
            tripId, latitude, longitude);

        // 1. Persist as TripLog entry in the database
        var logDto = new AddTripLogDto
        {
            Latitude  = latitude,
            Longitude = longitude,
            Address   = address,
            Timestamp = DateTime.UtcNow,
            Speed     = speed
        };

        TripLogDto savedLog;
        try
        {
            savedLog = await _tripService.AddLogAsync(tripId, logDto);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Failed to save TripLog for trip {TripId}: {Error}",
                tripId, ex.Message);

            // Tell the driver something went wrong
            await Clients.Caller.SendAsync("Error", ex.Message);
            return;
        }

        // 2. Broadcast to all subscribers of this trip
        // Everyone in the group named after tripId receives this
        await Clients.Group(tripId.ToString())
            .SendAsync("ReceiveLocationUpdate", new
            {
                tripId    = tripId,
                latitude  = latitude,
                longitude = longitude,
                address   = address,
                speed     = speed,
                timestamp = savedLog.Timestamp,
                logId     = savedLog.Id
            });

        _logger.LogInformation(
            "SIGNALR: Location update broadcast to group {TripId}",
            tripId);
    }
    
    // ── Connection lifecycle ─────────────────────────────────────────────────

    public override async Task OnConnectedAsync()
    {
        var user = Context.User?.Identity?.Name ?? "unknown";
        _logger.LogInformation(
            "SIGNALR: Client connected | ConnectionId: {ConnectionId} | User: {User}",
            Context.ConnectionId, user);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "SIGNALR: Client disconnected | ConnectionId: {ConnectionId}",
            Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

}