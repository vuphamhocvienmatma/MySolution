namespace ApiGateway.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; style-src 'self' 'unsafe-inline';");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "no-referrer");
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");

        await _next(context);
    }
}
