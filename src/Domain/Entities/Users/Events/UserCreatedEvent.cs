

namespace Domain.Users.Events;

public class UserCreatedEvent : BaseEvent
{
    public User User { get; }

    public UserCreatedEvent(User user)
    {
        User = user;
    }
}