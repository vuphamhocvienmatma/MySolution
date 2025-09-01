
namespace Application.Users.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cacheService;
    public CreateUserCommandHandler(IApplicationDbContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            DateOfBirth = request.DateOfBirth
        };

        var outboxMessage = new OutboxMessage
        {
            OccurredOnUtc = DateTime.UtcNow,
            Type = "UserCreated",
            Content = JsonSerializer.Serialize(new { user.Id, user.FirstName, user.LastName, user.Email })
        };
        user.AddDomainEvent(new UserCreatedEvent(user));
        _context.Users.Add(user);
        _context.OutboxMessages.Add(outboxMessage);
        await _context.SaveChangesAsync(cancellationToken);
        var cacheKey = $"user-{user.Id}";
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
        return user.Id;
    }
}