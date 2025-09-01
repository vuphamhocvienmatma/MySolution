using Application.Common.Interfaces;
using Infrastructure.Persistence.Services;

namespace WebAPI.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        var tenantId = context.User.Claims
            .FirstOrDefault(c => c.Type == "tenantId")?.Value;

        if (tenantId is not null && tenantService is TenantService concreteTenantService)
        {
            concreteTenantService.TenantId = tenantId;
        }

        await _next(context);
    }
}