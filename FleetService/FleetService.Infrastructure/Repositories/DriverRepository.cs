using FleetService.Application.Interfaces;
using FleetService.Domain.Entities;
using FleetService.Domain.Enums;
using FleetService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetService.Infrastructure.Repositories;

public class DriverRepository : IDriverRepository
{
    private readonly FleetDbContext _context;

    public DriverRepository(FleetDbContext context)
    {
        _context = context;
    }

    public async Task<Driver?> GetByIdAsync(Guid id)
    {
        return await _context.Drivers.FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<Driver>> GetAllAsync()
    {
        return await _context.Drivers.ToListAsync();
    }

    public async Task<IEnumerable<Driver>> GetByFilterAsync(DriverStatus? status)
    {
        IQueryable<Driver> query = _context.Drivers;

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        return await query.ToListAsync();
    }

    public async Task AddAsync(Driver driver)
    {
        await _context.Drivers.AddAsync(driver);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Driver driver)
    {
        _context.Drivers.Update(driver);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Driver driver)
    {
        _context.Drivers.Remove(driver);
        await _context.SaveChangesAsync();
    }
}