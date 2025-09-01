namespace Application.Common.Interfaces;
public interface INotificationService
{
    Task NotifyUserCreated(Guid userId, string fullName);
}