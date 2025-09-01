
namespace Infrastructure.Persistence.Mongo;

public class MongoRepository<TDocument> : IMongoRepository<TDocument> where TDocument : IDocument
{
    private readonly IMongoCollection<TDocument> _collection;
    private readonly string _tenantId;

    public MongoRepository(IMongoDatabase database, ITenantService tenantService)
    {
        _collection = database.GetCollection<TDocument>(typeof(TDocument).Name);
        _tenantId = tenantService.TenantId ?? throw new InvalidOperationException("TenantId is required for MongoDB operations.");
    }

    private FilterDefinition<TDocument> TenantFilter(FilterDefinition<TDocument> filter)
    {
        var tenantFilter = Builders<TDocument>.Filter.Eq(doc => doc.TenantId, _tenantId);
        return Builders<TDocument>.Filter.And(tenantFilter, filter);
    }

    public async Task<TDocument?> GetByIdAsync(string id)
    {
        var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, id);
        return await _collection.Find(TenantFilter(filter)).SingleOrDefaultAsync();
    }

    public async Task InsertOneAsync(TDocument document)
    {
        document.TenantId = _tenantId;
        await _collection.InsertOneAsync(document);
    }

    public async Task<IEnumerable<TDocument>> GetAllAsync()
    {
        var filter = Builders<TDocument>.Filter.Empty;
        var documents = await _collection.Find(TenantFilter(filter)).ToListAsync();
        return documents;
    }

    public async Task ReplaceOneAsync(TDocument document)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        document.TenantId = _tenantId;
        var filter = Builders<TDocument>.Filter.And(
            Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id),
            Builders<TDocument>.Filter.Eq(doc => doc.TenantId, _tenantId)
        );
        await _collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = false });
    }

    public async Task DeleteByIdAsync(string id)
    {
        var filter = Builders<TDocument>.Filter.And(
            Builders<TDocument>.Filter.Eq(doc => doc.Id, id),
            Builders<TDocument>.Filter.Eq(doc => doc.TenantId, _tenantId)
        );
        await _collection.DeleteOneAsync(filter);
    }
}