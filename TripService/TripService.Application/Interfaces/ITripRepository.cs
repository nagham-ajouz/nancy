using TripService.Domain.Entities;
using TripService.Domain.Enums;

namespace TripService.Application.Interfaces;

public interface ITripRepository
{
    Task<Trip?> GetByIdAsync(Guid id);
    // Include logs when we need distance calculation or log history
    Task<Trip?> GetByIdWithLogsAsync(Guid id);
    Task<IEnumerable<Trip>>  GetByFilterAsync(
        Guid?       driverId,
        Guid?       vehicleId,
        TripStatus? status,
        DateTime?   from,
        DateTime?   to);
    Task AddAsync(Trip trip);
    Task UpdateAsync(Trip trip);
    Task AddLogAsync(TripLog log);
}