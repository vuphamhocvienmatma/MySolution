using Domain.Entities.Users;
using Domain.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<OutboxMessage> OutboxMessages { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
