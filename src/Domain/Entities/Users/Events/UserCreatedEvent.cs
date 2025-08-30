using Domain.Common;
using Domain.Entities;
using Domain.Entities.Users;

namespace Domain.Users.Events;

public class UserCreatedEvent : BaseEvent
{
    public User User { get; }

    public UserCreatedEvent(User user)
    {
        User = user;
    }
}