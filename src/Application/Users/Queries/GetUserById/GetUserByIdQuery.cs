

namespace Application.Users.Queries.GetUserById;
public record GetUserByIdQuery(Guid Id) : ICacheableQuery<UserDto?>
{
    public string CacheKey => $"user-{Id}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}