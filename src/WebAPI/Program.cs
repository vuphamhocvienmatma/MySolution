using Application.Common.Behaviors;
using Application.Common.Interfaces;
using Elastic.Clients.Elasticsearch;
using FluentValidation;
using Hangfire;
using Infrastructure.BackgroundJobs;
using Infrastructure.Caching;
using Infrastructure.Configuration;
using Infrastructure.Http;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Configurations;
using Infrastructure.Persistence.Dapper;
using Infrastructure.Persistence.Mongo;
using Infrastructure.Persistence.Services;
using Infrastructure.Search;
using Infrastructure.Services;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using WebAPI.Middleware;

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

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

    builder.Services.AddStackExchangeRedisCache(options =>
        options.Configuration = builder.Configuration.GetConnectionString("Redis"));
    // Đổi tên đăng ký
    builder.Services.AddSingleton<IDistributedCacheService, DistributedCacheService>();

    builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration.GetConnectionString("MongoDb")));

    builder.Services.AddScoped<IMongoDatabase>(sp =>
    {
        var client = sp.GetRequiredService<IMongoClient>();
        return client.GetDatabase("MyMongoDatabaseName");
    });

    builder.Services.AddSingleton(sp => {
        var settings = sp.GetRequiredService<IOptions<ElasticsearchSettings>>().Value;
        var esSettings = new ElasticsearchClientSettings(new Uri(settings.Uri))
            .DefaultIndex("default-index");
        return new ElasticsearchClient(esSettings);

    }); 
    builder.Services.AddScoped<IElasticsearchService, ElasticsearchService>();

    builder.Services.AddScoped<ProcessOutboxMessagesJob>();
    builder.Services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));


    builder.Services.AddMassTransit(x =>
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            var settings = context.GetRequiredService<IOptions<MessageBrokerSettings>>().Value;

            cfg.Host(settings.Host, "/", h => {
                h.Username(settings.Username);
                h.Password(settings.Password);
            });

            cfg.ConfigureEndpoints(context);
        });
    });

    // Application
    builder.Services.AddValidatorsFromAssembly(Assembly.Load("Application"));
    builder.Services.AddMediatR(cfg => {
        cfg.RegisterServicesFromAssembly(Assembly.Load("Application"));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
    });
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<ITenantService, TenantService>();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
    builder.Services.AddMemoryCache();

    builder.Services.AddOptions<JwtSettings>()
     .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
     .ValidateDataAnnotations() 
     .ValidateOnStart(); 

    builder.Services.AddOptions<MessageBrokerSettings>()
        .Bind(builder.Configuration.GetSection(MessageBrokerSettings.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddOptions<ElasticsearchSettings>()
        .Bind(builder.Configuration.GetSection(ElasticsearchSettings.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // 3. Đăng ký Service Cache Đa Tầng
    builder.Services.AddScoped<ICacheService, MultiLayerCacheService>();
    // WebAPI
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options => {
        var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
    });
    builder.Services.AddAuthorization();

    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("fixed", opt =>
        {
            opt.PermitLimit = 10;
            opt.Window = TimeSpan.FromSeconds(10);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 2;
        });
        options.OnRejected = (context, _) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return new ValueTask();
        };
    });

    builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddHangfireServer();

    builder.Services.AddScoped<IHttpClientService, FlurlHttpClientService>();


    var retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError() 
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound) 
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))); 
    builder.Services.AddHttpClient("PaymentGateway", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ExternalServices:PaymentGateway:BaseUrl"]!);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    })
    .AddPolicyHandler(retryPolicy);

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<CorrelationIdMiddleware>(); 
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler(); 
    app.UseHttpsRedirection();
    app.UseRateLimiter();
    app.UseRouting();
    app.UseAuthentication();
    app.UseMiddleware<TenantResolutionMiddleware>();
    app.UseAuthorization();
    app.UseHangfireDashboard();
    app.MapControllers();

    RecurringJob.AddOrUpdate<ProcessOutboxMessagesJob>(
    "process-outbox-messages", 
    job => job.ExecuteAsync(),
    Cron.Minutely);
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