
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
        // 1. Thử lấy từ L1 Memory Cache
        if (_memoryCache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Cache L1 HIT for key: {CacheKey}", key);
            return value;
        }
        _logger.LogDebug("Cache L1 MISS for key: {CacheKey}", key);

        // 2. Thử lấy từ L2 Distributed Cache (Redis)
        var distributedValue = await _distributedCache.GetAsync<T>(key, cancellationToken);
        if (distributedValue is not null)
        {
            _logger.LogDebug("Cache L2 HIT for key: {CacheKey}", key);
            // Lưu lại vào L1 cho lần truy cập sau
            SetMemoryCache(key, distributedValue, expiration);
            return distributedValue;
        }
        _logger.LogDebug("Cache L2 MISS for key: {CacheKey}", key);

        // 3. Cache miss ở cả L1 và L2 -> Gọi factory để lấy từ nguồn (Database)
        _logger.LogDebug("Fetching from source for key: {CacheKey}", key);
        var sourceValue = await factory();

        if (sourceValue is not null)
        {
            // 4. Lưu vào cả L2 và L1
            await _distributedCache.SetAsync(key, sourceValue, expiration, cancellationToken);
            SetMemoryCache(key, sourceValue, expiration);
        }

        return sourceValue;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        // Xóa ở cả hai lớp cache
        _memoryCache.Remove(key);
        await _distributedCache.RemoveAsync(key, cancellationToken);
        _logger.LogDebug("Removed key {CacheKey} from L1 and L2 caches.", key);
    }

    private void SetMemoryCache<T>(string key, T value, TimeSpan? expiration)
    {
        var memoryCacheEntryOptions = new MemoryCacheEntryOptions
        {
            // L1 cache nên có thời gian sống ngắn hơn L2 một chút
            AbsoluteExpirationRelativeToNow = expiration.HasValue ? expiration.Value.Subtract(TimeSpan.FromSeconds(15)) : TimeSpan.FromMinutes(4)
        };
        _memoryCache.Set(key, value, memoryCacheEntryOptions);
    }
}