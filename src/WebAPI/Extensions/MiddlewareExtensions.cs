

namespace WebAPI.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app, IConfiguration configuration)
    {
        if (app is WebApplication webApp && webApp.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging();
        app.UseExceptionHandler();
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseHttpMetrics();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseMiddleware<TenantResolutionMiddleware>();
        app.UseAuthorization();
        app.UseHangfireDashboard();
        if (app is WebApplication webAppEndpoints)
        {
            webAppEndpoints.MapControllers();
            webAppEndpoints.MapHub<Hubs.NotificationHub>("/notificationHub");
            webAppEndpoints.MapMetrics();
        }

        return app;
    }

    public static void RegisterHangfireJobs(this IApplicationBuilder app)
    {
        RecurringJob.AddOrUpdate<ProcessOutboxMessagesJob>(
            "process-outbox-messages",
            job => job.ExecuteAsync(),
            Cron.Minutely);
    }
}