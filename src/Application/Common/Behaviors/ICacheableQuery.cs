using MediatR;

namespace Application.Common.Behaviors;

public interface ICacheableQuery<out TResponse> : IRequest<TResponse>
{
    string CacheKey { get; }
    TimeSpan? Expiration { get; }
}