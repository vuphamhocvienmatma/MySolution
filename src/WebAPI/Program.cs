using Application.Common.Behaviors;
using Application.Common.Interfaces;
using Elastic.Clients.Elasticsearch;
using FluentValidation;
using Infrastructure.BackgroundJobs;
using Infrastructure.Caching;
using Infrastructure.Persistence;
using Infrastructure.Search;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
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
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();

    builder.Services.AddSingleton(new ElasticsearchClient(
        new ElasticsearchClientSettings(new Uri(builder.Configuration["Elasticsearch:Uri"]!))));
    builder.Services.AddScoped<IElasticsearchService, ElasticsearchService>();

    builder.Services.AddHostedService<ProcessOutboxMessagesJob>();


    builder.Services.AddMassTransit(x =>
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(builder.Configuration["MessageBroker:Host"], "/", h => {
                h.Username(builder.Configuration["MessageBroker:Username"]);
                h.Password(builder.Configuration["MessageBroker:Password"]);
            });

            cfg.ConfigureEndpoints(context);
        });
    });

    // Application
    builder.Services.AddValidatorsFromAssembly(Assembly.Load("Application"));
    builder.Services.AddMediatR(cfg => {
        cfg.RegisterServicesFromAssembly(Assembly.Load("Application"));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
    });

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
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]!))
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
    app.UseAuthentication(); 
    app.UseAuthorization();
    app.MapControllers();

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