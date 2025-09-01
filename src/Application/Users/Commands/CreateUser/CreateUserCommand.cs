
namespace Application.Users.Commands.CreateUser;
[LoggingBehavior]
public record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    DateTime DateOfBirth) : IRequest<Guid>;