

namespace Application.Users.EventHandlers;

public class PublishUserIntegrationEventHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly ILogger<PublishUserIntegrationEventHandler> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly INotificationService _notificationService;

    public PublishUserIntegrationEventHandler(ILogger<PublishUserIntegrationEventHandler> logger, IPublishEndpoint publishEndpoint, INotificationService notificationService)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
        _notificationService = notificationService;
    }

    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Domain Event: UserCreatedEvent received, publishing integration event...");

        var integrationEvent = new UserCreatedIntegrationEvent
        {
            UserId = notification.User.Id,
            Email = notification.User.Email,
            FullName = $"{notification.User.FirstName} {notification.User.LastName}"
        };

        await _publishEndpoint.Publish(integrationEvent, cancellationToken);
        await _notificationService.NotifyUserCreated(
           notification.User.Id,
           $"{notification.User.FirstName} {notification.User.LastName}"
       );
        _logger.LogInformation("UserCreatedIntegrationEvent published for User ID: {UserId}", integrationEvent.UserId);
    }
}