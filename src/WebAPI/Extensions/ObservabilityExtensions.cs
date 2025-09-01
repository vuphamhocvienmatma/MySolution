

namespace WebAPI.Extensions;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: "MySolutionAPI", serviceVersion: "1.0.0");

        services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(opt => opt.SetDbStatementForText = true)
                    .AddSource("MassTransit")
                    .AddConsoleExporter()
                    .AddJaegerExporter(jaegerOptions =>
                    {
                        jaegerOptions.AgentHost = configuration.GetValue<string>("Jaeger:Host");
                        jaegerOptions.AgentPort = configuration.GetValue<int>("Jaeger:Port");
                    });
            });

        return services;
    }
}
