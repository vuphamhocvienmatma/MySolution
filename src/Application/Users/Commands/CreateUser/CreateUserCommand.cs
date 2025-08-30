using MediatR;
namespace Application.Users.Commands.CreateUser;

public record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    DateTime DateOfBirth) : IRequest<Guid>;