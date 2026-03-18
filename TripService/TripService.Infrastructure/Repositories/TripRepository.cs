using Microsoft.EntityFrameworkCore;
using TripService.Application.Interfaces;
using TripService.Domain.Entities;
using TripService.Domain.Enums;
using TripService.Infrastructure.Persistence;

namespace TripService.Infrastructure.Repositories;

public class TripRepository : ITripRepository
{
    private readonly TripDbContext _context;

    public TripRepository(TripDbContext context)
    {
        _context = context;
    }

    public async Task<Trip?> GetByIdAsync(Guid id)
    {
        return await _context.Trips
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    // Load trip with all its GPS logs — needed for distance calc and log history
    public async Task<Trip?> GetByIdWithLogsAsync(Guid id)
    {
        return await _context.Trips
            .Include(t => t.Logs)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<Trip>> GetByFilterAsync(
        Guid?       driverId,
        Guid?       vehicleId,
        TripStatus? status,
        DateTime?   from,
        DateTime?   to)
    {
        IQueryable<Trip> query = _context.Trips;

        if (driverId.HasValue)
            query = query.Where(t => t.DriverId == driverId.Value);

        if (vehicleId.HasValue)
            query = query.Where(t => t.VehicleId == vehicleId.Value);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (from.HasValue)
            query = query.Where(t => t.StartTime >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.StartTime <= to.Value);

        return await query.ToListAsync();
    }

    public async Task AddAsync(Trip trip)
    {
        await _context.Trips.AddAsync(trip);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Trip trip)
    {
        // Detach any already-tracked instance to avoid conflicts
        var tracked = _context.ChangeTracker.Entries<Trip>()
            .FirstOrDefault(e => e.Entity.Id == trip.Id);

        if (tracked != null)
            tracked.State = EntityState.Detached;

        _context.Trips.Update(trip);
        await _context.SaveChangesAsync();
    }
    
    public async Task AddLogAsync(TripLog log)
    {
        // Save the log directly — avoids concurrency issues with the parent trip
        await _context.TripLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }
}