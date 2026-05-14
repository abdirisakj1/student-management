using MongoDB.Driver;
using SmartWasteManagement.Data;
using SmartWasteManagement.Models;
using Microsoft.Extensions.Options;

namespace SmartWasteManagement.Services;

public interface IPlaceService
{
    Task<List<Place>> GetAllAsync();
    Task<Place?> GetByIdAsync(string id);
    Task<Place> CreateAsync(Place place);
    Task<bool> UpdateAsync(string id, Place place);
    Task<bool> DeleteAsync(string id);
}

/// <summary>
/// MongoDB-backed waste place / report repository.
/// </summary>
public class PlaceService : IPlaceService
{
    private readonly IMongoCollection<Place> _places;

    public PlaceService(IMongoDatabase database, IOptions<MongoDbSettings> options)
    {
        _places = database.GetCollection<Place>(options.Value.PlacesCollectionName);
    }

    public async Task<List<Place>> GetAllAsync() =>
        await _places.Find(_ => true).SortByDescending(p => p.CreatedAt).ToListAsync();

    public async Task<Place?> GetByIdAsync(string id)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return null;
        return await _places.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Place> CreateAsync(Place place)
    {
        place.CreatedAt = DateTime.UtcNow;
        await _places.InsertOneAsync(place);
        return place;
    }

    public async Task<bool> UpdateAsync(string id, Place place)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return false;
        var existing = await _places.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (existing is null)
            return false;
        place.Id = id;
        place.CreatedAt = existing.CreatedAt;
        var result = await _places.ReplaceOneAsync(p => p.Id == id, place);
        return result.ModifiedCount > 0 || result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return false;
        var result = await _places.DeleteOneAsync(p => p.Id == id);
        return result.DeletedCount > 0;
    }
}
