using WebAPI.Hubs;

namespace WebAPI.Services;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyUserCreated(Guid userId, string fullName)
    {
        await _hubContext.Clients.All.SendAsync("NewUserCreated", new { UserId = userId, FullName = fullName });
    }
}