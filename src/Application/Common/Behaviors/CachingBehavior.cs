using Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Common.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheableQuery<TResponse>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(ICacheService cacheService, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking cache for key: {CacheKey}", request.CacheKey);

        var cachedResponse = await _cacheService.GetAsync<TResponse>(request.CacheKey, cancellationToken);
        if (cachedResponse is not null)
        {
            _logger.LogInformation("Cache hit for key: {CacheKey}", request.CacheKey);
            return cachedResponse;
        }

        _logger.LogInformation("Cache miss for key: {CacheKey}. Fetching from source.", request.CacheKey);

        var response = await next();

        if (response is not null)
        {
            await _cacheService.SetAsync(request.CacheKey, response, request.Expiration, cancellationToken);
            _logger.LogInformation("Set cache for key: {CacheKey}", request.CacheKey);
        }

        return response;
    }
}