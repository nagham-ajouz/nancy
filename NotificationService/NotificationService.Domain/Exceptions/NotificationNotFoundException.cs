namespace NotificationService.Domain.Exceptions;

public class NotificationNotFoundException : Exception
{
    public NotificationNotFoundException(Guid id)
        : base($"Notification {id} was not found.") { }
}