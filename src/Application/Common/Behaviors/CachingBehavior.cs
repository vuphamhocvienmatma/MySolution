

namespace Application.Common.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheableQuery<TResponse> // Giữ nguyên ICacheableQuery interface
{
    private readonly ICacheService _cacheService; // Dùng ICacheService đa tầng
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(ICacheService cacheService, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to get from cache for key: {CacheKey}", request.CacheKey);

        // Toàn bộ logic phức tạp đã nằm gọn trong GetOrCreateAsync
        return await _cacheService.GetOrCreateAsync(
            request.CacheKey,
            () => next(), // Hàm factory chính là việc gọi handler tiếp theo trong pipeline
            request.Expiration,
            cancellationToken);
    }
}