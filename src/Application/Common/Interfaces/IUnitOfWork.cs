using Domain.Entities.Users;

namespace Application.Common.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserDapperRepository Users { get; }

    void BeginTransaction();
    void Commit();
    void Rollback();
}

public interface IUserDapperRepository : IDapperRepository<User> { }