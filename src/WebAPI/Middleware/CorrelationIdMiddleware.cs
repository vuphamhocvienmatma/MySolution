namespace WebAPI.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault() ?? Guid.NewGuid().ToString();

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);
                return Task.CompletedTask;
            });
            await _next(context);
        }
    }
}