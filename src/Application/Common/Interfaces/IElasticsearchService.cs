using Domain.Entities;
using Domain.Entities.Users;

namespace Application.Common.Interfaces;

public interface IElasticsearchService
{
    Task IndexUserAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(User user, CancellationToken cancellationToken = default);
}