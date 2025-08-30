using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities.Users;
using Domain.Outbox;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly IPublisher _mediator;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IPublisher mediator) // Inject IPublisher
            : base(options)
    {
        _mediator = mediator;
    }
    public DbSet<User> Users => Set<User>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
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
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedOnUtc = DateTime.UtcNow;
                    break;
            }
        }
        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in events)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
        return result;
    }
}