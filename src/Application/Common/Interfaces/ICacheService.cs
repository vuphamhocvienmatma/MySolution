namespace Application.Common.Interfaces;

public interface ICacheService
{
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory, 
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}