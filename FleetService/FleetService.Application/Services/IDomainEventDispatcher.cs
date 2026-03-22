using Shared.BaseClasses;

namespace FleetService.Application.Services;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(Entity entity);
}