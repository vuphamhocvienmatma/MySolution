Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services
        .AddConfigurationSettings(builder.Configuration)
        .AddApplicationServices()
        .AddInfrastructureServices(builder.Configuration)
        .AddWebAPIServices(builder.Configuration);
    var app = builder.Build();
    app.UseInfrastructure(builder.Configuration);
    app.RegisterHangfireJobs();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}