using MongoDB.Driver;
using SmartWasteManagement.Data;
using SmartWasteManagement.Models;
using Microsoft.Extensions.Options;

namespace SmartWasteManagement.Services;

public interface IRouteService
{
    Task<List<RoutePlan>> GetAllAsync();
    Task<RoutePlan?> GetByIdAsync(string id);
    Task<List<RoutePlan>> GetByDriverIdAsync(string driverId);
    Task<RoutePlan> CreateAsync(RoutePlan route);
    Task<bool> UpdateAsync(string id, RoutePlan route);
    Task<bool> DeleteAsync(string id);
}

public class RouteService : IRouteService
{
    private readonly IMongoCollection<RoutePlan> _routes;

    public RouteService(IMongoDatabase database, IOptions<MongoDbSettings> options)
    {
        _routes = database.GetCollection<RoutePlan>(options.Value.RoutesCollectionName);
    }

    public async Task<List<RoutePlan>> GetAllAsync() =>
        await _routes.Find(_ => true).ToListAsync();

    public async Task<RoutePlan?> GetByIdAsync(string id)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return null;
        return await _routes.Find(r => r.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<RoutePlan>> GetByDriverIdAsync(string driverId) =>
        await _routes.Find(r => r.AssignedDriverId == driverId).ToListAsync();

    public async Task<RoutePlan> CreateAsync(RoutePlan route)
    {
        await _routes.InsertOneAsync(route);
        return route;
    }

    public async Task<bool> UpdateAsync(string id, RoutePlan route)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return false;
        route.Id = id;
        var result = await _routes.ReplaceOneAsync(r => r.Id == id, route);
        return result.ModifiedCount > 0 || result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return false;
        var result = await _routes.DeleteOneAsync(r => r.Id == id);
        return result.DeletedCount > 0;
    }
}
