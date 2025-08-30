namespace Application.Users.Queries.GetUserById;
public class UserDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public int Age { get; init; }
}