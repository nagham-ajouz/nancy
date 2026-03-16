using FleetService.Domain.Entities;
using FleetService.Domain.Enums;
using FleetService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetService.Infrastructure.Repositories;

public class VehicleRepository
{
    private readonly FleetDbContext _context;

    public VehicleRepository(FleetDbContext context)
    {
        _context = context;
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id)
    {
        return await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<IEnumerable<Vehicle>> GetAllAsync()
    {
        return await _context.Vehicles.ToListAsync();
    }

    public async Task<IEnumerable<Vehicle>> GetByFilterAsync(VehicleStatus? status, VehicleType? type)
    {
        // IQueryable allows LINQ queries to be translated into SQL and executed in the database instead of in memory.
        IQueryable<Vehicle> query = _context.Vehicles;

        if (status.HasValue)
            query = query.Where(v => v.Status == status.Value);

        if (type.HasValue)
            query = query.Where(v => v.Type == type.Value);

        return await query.ToListAsync();
    }

    public async Task AddAsync(Vehicle vehicle)
    {
        await _context.Vehicles.AddAsync(vehicle);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Vehicle vehicle)
    {
        _context.Vehicles.Update(vehicle);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Vehicle vehicle)
    {
        _context.Vehicles.Remove(vehicle);
        await _context.SaveChangesAsync();
    }
}