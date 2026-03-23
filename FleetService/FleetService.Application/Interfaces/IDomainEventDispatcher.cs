using Shared.BaseClasses;

namespace FleetService.Interfaces.Services;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(Entity entity);
}