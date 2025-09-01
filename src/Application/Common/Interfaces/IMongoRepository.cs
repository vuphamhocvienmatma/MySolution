using Domain.Common;
using System.Linq.Expressions;

namespace Application.Common.Interfaces;

public interface IMongoRepository<TDocument> where TDocument : IDocument
{
    Task<TDocument?> GetByIdAsync(string id);
    Task<IEnumerable<TDocument>> GetAllAsync();
    Task InsertOneAsync(TDocument document);
    Task ReplaceOneAsync(TDocument document);
    Task DeleteByIdAsync(string id);
}