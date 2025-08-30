using Application.Common.Contracts;
using Domain.Users.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Users.EventHandlers;

public class PublishUserIntegrationEventHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly ILogger<PublishUserIntegrationEventHandler> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public PublishUserIntegrationEventHandler(ILogger<PublishUserIntegrationEventHandler> logger, IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Domain Event: UserCreatedEvent received, publishing integration event...");

        // Tạo message contract
        var integrationEvent = new UserCreatedIntegrationEvent
        {
            UserId = notification.User.Id,
            Email = notification.User.Email,
            FullName = $"{notification.User.FirstName} {notification.User.LastName}"
        };

        // Dùng MassTransit để publish message
        await _publishEndpoint.Publish(integrationEvent, cancellationToken);

        _logger.LogInformation("UserCreatedIntegrationEvent published for User ID: {UserId}", integrationEvent.UserId);
    }
}