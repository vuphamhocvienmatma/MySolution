using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Entities.Users;
using Elastic.Clients.Elasticsearch;

namespace Infrastructure.Search;

public class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private const string UsersIndexName = "users";

    public ElasticsearchService(ElasticsearchClient client)
    {
        _client = client;
    }

    public async Task IndexUserAsync(User user, CancellationToken cancellationToken = default)
    {
        await _client.IndexAsync(user, UsersIndexName, cancellationToken);
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _client.DeleteAsync(UsersIndexName, userId, cancellationToken);
    }

    public async Task UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        await _client.UpdateAsync<User, object>(UsersIndexName, user.Id, u => u.Doc(user), cancellationToken);
    }
}