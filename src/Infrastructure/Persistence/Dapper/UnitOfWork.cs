

namespace Infrastructure.Persistence.Dapper;

public class UnitOfWork : IUnitOfWork
{
    private IDbConnection _connection;
    private IDbTransaction _transaction;
    private bool _disposed;
    private ITenantService _tenantService;
    public IUserDapperRepository Users { get; }

    public UnitOfWork(IConfiguration configuration, ITenantService tenantService)
    {
        _connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        _connection.Open();
        _transaction = _connection.BeginTransaction();
        _tenantService = tenantService;
        string tenantId = _tenantService.TenantId ?? throw new InvalidOperationException("TenantId is required for Dapper operations.");

        Users = new UserDapperRepository(_transaction, tenantId);
    }

    public void BeginTransaction()
    {
        _transaction = _connection.BeginTransaction();
        ((UserDapperRepository)Users).SetTransaction(_transaction);
    }

    public void Commit()
    {
        try
        {
            _transaction.Commit();
        }
        catch
        {
            _transaction.Rollback();
            throw;
        }
        finally
        {
            _transaction.Dispose();
        }
    }

    public void Rollback()
    {
        _transaction.Rollback();
        _transaction.Dispose();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _transaction?.Dispose();
            _connection?.Dispose();
            _disposed = true;
        }
    }
}