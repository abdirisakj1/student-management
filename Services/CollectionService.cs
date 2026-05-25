using MongoDB.Driver;
using SmartWasteManagement.Data;
using SmartWasteManagement.Models;
using Microsoft.Extensions.Options;

namespace SmartWasteManagement.Services;

public interface ICollectionService
{
    Task<List<CollectionRecord>> GetAllAsync();
    Task<List<CollectionRecord>> GetByDriverIdAsync(string driverId);
    Task<CollectionRecord?> GetByIdAsync(string id);
    Task<CollectionRecord> CreateAsync(CollectionRecord record);
    Task<bool> UpdateAsync(string id, CollectionRecord record);
}

public class CollectionService : ICollectionService
{
    private readonly IMongoCollection<CollectionRecord> _collections;

    public CollectionService(IMongoDatabase database, IOptions<MongoDbSettings> options)
    {
        _collections = database.GetCollection<CollectionRecord>(options.Value.CollectionsCollectionName);
    }

    public async Task<List<CollectionRecord>> GetAllAsync() =>
        await _collections.Find(_ => true).SortByDescending(c => c.CollectedAt).ToListAsync();

    public async Task<List<CollectionRecord>> GetByDriverIdAsync(string driverId) =>
        await _collections.Find(c => c.DriverId == driverId)
            .SortByDescending(c => c.CollectedAt).ToListAsync();

    public async Task<CollectionRecord?> GetByIdAsync(string id)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return null;
        return await _collections.Find(c => c.Id == id).FirstOrDefaultAsync();
    }

    public async Task<CollectionRecord> CreateAsync(CollectionRecord record)
    {
        await _collections.InsertOneAsync(record);
        return record;
    }

    public async Task<bool> UpdateAsync(string id, CollectionRecord record)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return false;
        record.Id = id;
        var result = await _collections.ReplaceOneAsync(c => c.Id == id, record);
        return result.ModifiedCount > 0 || result.MatchedCount > 0;
    }
}
