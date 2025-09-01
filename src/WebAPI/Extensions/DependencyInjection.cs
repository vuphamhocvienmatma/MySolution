namespace WebAPI.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddConfigurationSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();

        services.AddOptions<MessageBrokerSettings>()
            .Bind(configuration.GetSection(MessageBrokerSettings.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();

        services.AddOptions<ElasticsearchSettings>()
            .Bind(configuration.GetSection(ElasticsearchSettings.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();

        return services;
    }
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.Load("Application"));
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.Load("Application"));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
        });

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();

        services.AddOptions<MessageBrokerSettings>()
            .Bind(configuration.GetSection(MessageBrokerSettings.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();

        services.AddOptions<ElasticsearchSettings>()
            .Bind(configuration.GetSection(ElasticsearchSettings.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();

        // EF Core
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Dapper
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // MongoDB
        services.AddSingleton<IMongoClient>(sp => new MongoClient(configuration.GetConnectionString("MongoDb")));
        services.AddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase("MyMongoDatabaseName"));
        services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));

        // Caching
        services.AddMemoryCache();
        services.AddStackExchangeRedisCache(options => options.Configuration = configuration.GetConnectionString("Redis"));
        services.AddSingleton<IDistributedCacheService, DistributedCacheService>();
        services.AddScoped<ICacheService, MultiLayerCacheService>();

        // Search
        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<ElasticsearchSettings>>().Value;
            var esSettings = new ElasticsearchClientSettings(new Uri(settings.Uri)).DefaultIndex("default-index");
            return new ElasticsearchClient(esSettings);
        });
        services.AddScoped<IElasticsearchService, ElasticsearchService>();

        // Messaging - MassTransit
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                var settings = context.GetRequiredService<IOptions<MessageBrokerSettings>>().Value;

                cfg.Host(settings.Host, "/", h =>
                {
                    h.Username(settings.Username);
                    h.Password(settings.Password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        // HTTP Client
        var retryPolicy = HttpPolicyExtensions.HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        services.AddHttpClient("PaymentGateway", client =>
        {
            client.BaseAddress = new Uri(configuration["ExternalServices:PaymentGateway:BaseUrl"]!);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }).AddPolicyHandler(retryPolicy);

        // Custom Services
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IHttpClientService, FlurlHttpClientService>();

        return services;
    }

    public static IServiceCollection AddWebAPIServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddHttpContextAccessor();

        // Swagger / OpenAPI
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // Real-time
        services.AddSignalR();
        services.AddScoped<INotificationService, SignalRNotificationService>();

        // Exception Handling & Security
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;

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
        services.AddAuthorization();
        services.AddRateLimiter(options =>
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
        ;

        // Background Jobs - Hangfire
        services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection")));
        services.AddHangfireServer();
        services.AddScoped<ProcessOutboxMessagesJob>();

        return services;
    }
}