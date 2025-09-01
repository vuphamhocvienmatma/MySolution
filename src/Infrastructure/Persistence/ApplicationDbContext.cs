

namespace Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly IPublisher _mediator;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IPublisher mediator, ITenantService tenantService, ICurrentUserService currentUserService)
            : base(options)
    {
        _mediator = mediator;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }
    public DbSet<User> Users => Set<User>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddQueryFilter<IMustHaveTenant>(e => e.TenantId == _tenantService.TenantId);

    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {

        var events = ChangeTracker.Entries<BaseAuditableEntity>()
            .Select(e => e.Entity.DomainEvents)
            .SelectMany(e => e)
            .ToList();

        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedOnUtc = DateTime.UtcNow;
                    entry.Entity.CreatedBy = _currentUserService.UserId;

                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedOnUtc = DateTime.UtcNow;
                    entry.Entity.LastModifiedBy = _currentUserService.UserId;

                    break;
            }
        }
        var result = await base.SaveChangesAsync(cancellationToken);
        foreach (var domainEvent in events)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
        foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = _tenantService.TenantId
                    ?? throw new InvalidOperationException("TenantId cannot be null.");
            }
        }
        return result;
    }
}

