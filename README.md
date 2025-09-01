Project Readme: .NET 9 Clean Architecture API
This document provides a comprehensive guide to the project's architecture, development workflows, and common services. It's intended for developers joining the project to get up to speed quickly.

1. Core Architecture 🏛️
This project is built upon Clean Architecture principles to create a decoupled, maintainable, and testable system. The core idea is that business logic should not depend on technical implementation details.

The Dependency Rule: WebAPI → Infrastructure → Application → Domain

Project Layers
Library: Central of Nuget Package

Domain: The heart of the application. It contains the core business logic, entities, and domain events. It has no dependencies on other layers.

Application: Contains the application's use cases, orchestrated via the CQRS pattern (Commands and Queries). It defines interfaces for infrastructure concerns but does not implement them.

Infrastructure: Implements the interfaces defined in the Application layer. It contains all the technical details like database access (EF Core, Dapper), caching (Redis), external API calls (Flurl), and background job processing (Hangfire).

WebAPI: The entry point. It exposes the application's features via a RESTful API. Controllers here are kept "thin" by simply receiving HTTP requests and dispatching commands or queries to MediatR.

Key Technologies
Area	Technology / Pattern
Architecture	Clean Architecture, DDD, CQRS
Database	EF Core, Dapper, MongoDB
Messaging	MassTransit, RabbitMQ, Outbox Pattern
Caching	Multi-Layer Cache (In-Memory + Redis)
Background Jobs	Hangfire
API Security	JWT, Rate Limiting, Multi-Tenancy
Logging	Serilog with Correlation ID


2. Getting Started 🚀
Prerequisites
.NET 9 SDK

Docker (for running database, Redis, RabbitMQ)

A SQL Server instance

Configuration
Clone the repository.

Open src/WebAPI/appsettings.Development.json.

Update the ConnectionStrings for your SQL Server and Redis instances.

Ensure all other settings (JwtSettings, MessageBroker, etc.) are configured as needed.

Running the Project
Bash

# Navigate to the WebAPI project directory
cd src/WebAPI

# Run the application
dotnet run
The API will be available at https://localhost:7001 (or similar), and you can access the Swagger UI at /swagger. The Hangfire dashboard is available at /hangfire.

3. Developer Workflows 👨‍💻
Database Management with EF Core
All commands should be run from the root of the repository.

Creating a Migration: After changing a domain entity, create a migration to capture the schema change.

Bash

dotnet ef migrations add <YourMigrationName> --project src/Infrastructure --startup-project src/WebAPI
Applying Migrations: To apply pending migrations to the database.

Bash

dotnet ef database update --startup-project src/WebAPI
How to Add a New Feature (e.g., "Create Product")
This workflow demonstrates the vertical slice approach.

Domain Layer:

Create the entity file: src/Domain/Entities/Product.cs.

C#

public class Product : BaseAuditableEntity, IMustHaveTenant
{
    public string TenantId { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
}
Application Layer:

Create a new folder: src/Application/Products/Commands/CreateProduct.

Define the command: CreateProductCommand.cs.

C#

public record CreateProductCommand(string Name, decimal Price) : IRequest<Guid>;
Define the validator: CreateProductCommandValidator.cs.

C#

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand> { /* ... rules ... */ }
Implement the handler: CreateProductCommandHandler.cs. This contains the core logic.

C#

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    public CreateProductCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var product = new Product { /* ... */ };
        _context.Products.Add(product);
        // The Outbox message is added automatically if needed via other mechanisms
        await _context.SaveChangesAsync(ct);
        return product.Id;
    }
}
Infrastructure Layer:

Update IApplicationDbContext and ApplicationDbContext to include DbSet<Product> Products { get; }.

Create an EF Core configuration file: src/Infrastructure/Persistence/Configurations/ProductConfiguration.cs.

C#

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
    }
}
WebAPI Layer:

Add a new method to ProductsController.cs.

C#

[HttpPost]
public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command)
{
    var productId = await Mediator.Send(command);
    return CreatedAtAction(nameof(GetProductById), new { id = productId }, new { id = productId });
}
Final Step: Create and apply the EF Core migration.

4. How to Use Common Services 🛠️
This project provides several shared services to handle common tasks consistently. You can inject these interfaces into your handlers or other services.

ICurrentUserService
Provides information about the authenticated user making the request.

Purpose: To get the current user's ID, Tenant ID, email, etc., without accessing HttpContext directly in your business logic.

Usage (in a Command Handler):

C#

private readonly ICurrentUserService _currentUser;
public MyHandler(ICurrentUserService currentUser) => _currentUser = currentUser;

public async Task Handle(...)
{
    var userId = _currentUser.UserId;
    var tenantId = _currentUser.TenantId;
    // The CreatedBy field is usually set automatically in the DbContext SaveChangesAsync override.
}
ICacheService
A multi-layer cache service (In-Memory + Redis) for high-performance data retrieval.

Purpose: To cache data that is read frequently but changes infrequently. It automatically handles checking L1 (memory) then L2 (Redis) before hitting the database.

Usage (in a Query Handler or MediatR Behavior): The CachingBehavior already uses this. To use it manually:

C#

private readonly ICacheService _cache;
public MyQueryHandler(ICacheService cache) => _cache = cache;

public async Task Handle(...)
{
    string cacheKey = "my-unique-data-key";
    var data = await _cache.GetOrCreateAsync(
        cacheKey,
        () => _dbContext.Products.ToListAsync(), // This function only runs on a cache miss
        TimeSpan.FromMinutes(10)
    );
    return data;
}
IHttpClientService
A generic client for making external HTTP calls using Flurl, with built-in resilience via Polly.

Purpose: To call third-party APIs in a standardized, testable, and resilient way.

Usage (in a Command Handler):

C#

private readonly IHttpClientService _httpClient;
public MyPaymentHandler(IHttpClientService httpClient) => _httpClient = httpClient;

public async Task Handle(...)
{
    var response = await _httpClient.PostAsync<PaymentRequest, PaymentResponse>(
        clientName: "PaymentGateway", // Must be configured in Program.cs
        requestUri: "process-payment",
        data: new PaymentRequest { /* ... */ }
    );
    // ... process the response
}
5. Background Jobs with Hangfire 🕒
Hangfire is used for reliable background job processing.

Purpose: To offload long-running, non-critical tasks from the API request thread (e.g., sending emails, processing reports, syncing data).

Dashboard: You can monitor jobs at the /hangfire endpoint.

How to Schedule a Job
Fire-and-Forget: Executes once, immediately in the background. Good for tasks like sending a welcome email.

C#

// Inject IBackgroundJobClient
private readonly IBackgroundJobClient _jobClient;

// Inside a handler
_jobClient.Enqueue<IEmailService>(emailService => emailService.SendWelcomeEmail(userId));
Delayed: Executes once after a specified delay.

C#

_jobClient.Schedule<IReportingService>(
    reportingService => reportingService.GenerateMonthlyReport(tenantId),
    TimeSpan.FromHours(2)
);
Recurring: Executes repeatedly on a CRON schedule. This is how the ProcessOutboxMessagesJob is configured in Program.cs.

C#

// In Program.cs
RecurringJob.AddOrUpdate<ProcessOutboxMessagesJob>(
    "process-outbox-messages",
    job => job.ExecuteAsync(),
    Cron.Minutely // Runs every minute
);