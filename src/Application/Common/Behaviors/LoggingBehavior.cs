

namespace Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var attribute = typeof(TRequest).GetCustomAttribute<LoggingBehaviorAttribute>();

        if (attribute is null)
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Executing request {RequestName}...", requestName);

        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        _logger.LogInformation("Finished request {RequestName} in {ElapsedMilliseconds}ms.", requestName, stopwatch.ElapsedMilliseconds);

        return response;
    }
}