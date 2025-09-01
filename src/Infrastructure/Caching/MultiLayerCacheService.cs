
namespace Infrastructure.Caching;

public class MultiLayerCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache; // L1 Cache
    private readonly IDistributedCacheService _distributedCache; // L2 Cache
    private readonly ILogger<MultiLayerCacheService> _logger;

    public MultiLayerCacheService(
        IMemoryCache memoryCache,
        IDistributedCacheService distributedCache,
        ILogger<MultiLayerCacheService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (_memoryCache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Cache L1 HIT for key: {CacheKey}", key);
            return value;
        }
        _logger.LogDebug("Cache L1 MISS for key: {CacheKey}", key);

        var distributedValue = await _distributedCache.GetAsync<T>(key, cancellationToken);
        if (distributedValue is not null)
        {
            _logger.LogDebug("Cache L2 HIT for key: {CacheKey}", key);
            SetMemoryCache(key, distributedValue, expiration);
            return distributedValue;
        }
        _logger.LogDebug("Cache L2 MISS for key: {CacheKey}", key);

        _logger.LogDebug("Fetching from source for key: {CacheKey}", key);
        var sourceValue = await factory();

        if (sourceValue is not null)
        {
            await _distributedCache.SetAsync(key, sourceValue, expiration, cancellationToken);
            SetMemoryCache(key, sourceValue, expiration);
        }

        return sourceValue;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        await _distributedCache.RemoveAsync(key, cancellationToken);
        _logger.LogDebug("Removed key {CacheKey} from L1 and L2 caches.", key);
    }

    private void SetMemoryCache<T>(string key, T value, TimeSpan? expiration)
    {
        var memoryCacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration.HasValue ? expiration.Value.Subtract(TimeSpan.FromSeconds(15)) : TimeSpan.FromMinutes(4)
        };
        _memoryCache.Set(key, value, memoryCacheEntryOptions);
    }
}