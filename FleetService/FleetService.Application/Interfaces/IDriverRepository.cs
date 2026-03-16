using FleetService.Domain.Entities;
using FleetService.Domain.Enums;

namespace FleetService.Application.Interfaces;

public interface IDriverRepository
{
    Task<Driver?> GetByIdAsync(Guid id);
    Task<IEnumerable<Driver>> GetAllAsync();
    Task<IEnumerable<Driver>> GetByFilterAsync(DriverStatus? status);
    Task AddAsync(Driver driver);
    Task UpdateAsync(Driver driver);
    Task DeleteAsync(Driver driver);
}