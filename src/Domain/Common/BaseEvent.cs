using MediatR;

namespace Domain.Common;

// INotification là một marker interface từ MediatR
public abstract class BaseEvent : INotification
{
}