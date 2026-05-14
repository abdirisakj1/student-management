using MongoDB.Driver;
using SmartWasteManagement.Data;
using SmartWasteManagement.Models;
using Microsoft.Extensions.Options;

namespace SmartWasteManagement.Services;

public interface ITruckService
{
    Task<List<Truck>> GetAllAsync();
    Task<Truck?> GetByIdAsync(string id);
    Task<Truck> CreateAsync(Truck truck);
    Task<bool> UpdateAsync(string id, Truck truck);
    Task<bool> DeleteAsync(string id);
}

/// <summary>
/// MongoDB-backed truck repository.
/// </summary>
public class TruckService : ITruckService
{
    private readonly IMongoCollection<Truck> _trucks;

    public TruckService(IMongoDatabase database, IOptions<MongoDbSettings> options)
    {
        _trucks = database.GetCollection<Truck>(options.Value.TrucksCollectionName);
    }

    public async Task<List<Truck>> GetAllAsync() =>
        await _trucks.Find(_ => true).ToListAsync();

    public async Task<Truck?> GetByIdAsync(string id)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return null;
        return await _trucks.Find(t => t.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Truck> CreateAsync(Truck truck)
    {
        await _trucks.InsertOneAsync(truck);
        return truck;
    }

    public async Task<bool> UpdateAsync(string id, Truck truck)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return false;
        truck.Id = id;
        var result = await _trucks.ReplaceOneAsync(t => t.Id == id, truck);
        return result.ModifiedCount > 0 || result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return false;
        var result = await _trucks.DeleteOneAsync(t => t.Id == id);
        return result.DeletedCount > 0;
    }
}
