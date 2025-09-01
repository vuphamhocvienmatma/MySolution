using Application.Common.Interfaces;
using Dapper;
using Domain.Entities;
using Domain.Entities.Users;
using Elastic.Clients.Elasticsearch;
using System.Data;

namespace Infrastructure.Persistence.Dapper;

public class UserDapperRepository : IUserDapperRepository
{
    private IDbTransaction _transaction;
    private IDbConnection _connection => _transaction.Connection;
    private readonly string _tenantId;

    public UserDapperRepository(IDbTransaction transaction, string tenantId)
    {
        _transaction = transaction;
        _tenantId = tenantId;
    }
    public void SetTransaction(IDbTransaction transaction) => _transaction = transaction;

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _connection.QuerySingleOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Id = @IdAND TenantId = @TenantId",
            new { Id = id, TenantId = _tenantId }, _transaction);
    }

    public async Task<Guid> AddAsync(User entity)
    {
        var sql = "INSERT INTO Users (Id, FirstName, LastName, Email, DateOfBirth, TenantId) VALUES (@Id, @FirstName, @LastName, @Email, @DateOfBirth, @TenantId)";
        await _connection.ExecuteAsync(sql, entity, _transaction);
        return entity.Id;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _connection.QueryAsync<User>(
            "SELECT * FROM Users", null, _transaction);
    }

    public async Task UpdateAsync(User entity)
    {
        var sql = @"UPDATE Users 
                    SET FirstName = @FirstName, 
                        LastName = @LastName, 
                        Email = @Email, 
                        DateOfBirth = @DateOfBirth, 
                        LastModifiedOnUtc = @LastModifiedOnUtc, 
                        LastModifiedBy = @LastModifiedBy
                    WHERE Id = @Id";
        await _connection.ExecuteAsync(sql, new
        {
            entity.FirstName,
            entity.LastName,
            entity.Email,
            entity.DateOfBirth,
            entity.LastModifiedOnUtc,
            entity.LastModifiedBy,
            entity.Id
        }, _transaction);
    }

    public async Task DeleteAsync(Guid id)
    {
         await _connection.ExecuteAsync(
           "DELETE FROM Users WHERE Id = @Id", new { Id = id }, _transaction);
    }
}