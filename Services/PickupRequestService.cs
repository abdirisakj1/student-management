using MongoDB.Driver;
using SmartWasteManagement.Data;
using SmartWasteManagement.Models;
using Microsoft.Extensions.Options;

namespace SmartWasteManagement.Services;

public interface IPickupRequestService
{
    Task<List<PickupRequest>> GetAllAsync();
    Task<List<PickupRequest>> GetByCustomerIdAsync(string customerId);
    Task<PickupRequest?> GetByIdAsync(string id);
    Task<PickupRequest> CreateAsync(PickupRequest request);
    Task<bool> UpdateAsync(string id, PickupRequest request);
}

public class PickupRequestService : IPickupRequestService
{
    private readonly IMongoCollection<PickupRequest> _requests;

    public PickupRequestService(IMongoDatabase database, IOptions<MongoDbSettings> options)
    {
        _requests = database.GetCollection<PickupRequest>(options.Value.PickupRequestsCollectionName);
    }

    public async Task<List<PickupRequest>> GetAllAsync() =>
        await _requests.Find(_ => true).SortByDescending(r => r.CreatedAt).ToListAsync();

    public async Task<List<PickupRequest>> GetByCustomerIdAsync(string customerId) =>
        await _requests.Find(r => r.CustomerId == customerId)
            .SortByDescending(r => r.CreatedAt).ToListAsync();

    public async Task<PickupRequest?> GetByIdAsync(string id)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return null;
        return await _requests.Find(r => r.Id == id).FirstOrDefaultAsync();
    }

    public async Task<PickupRequest> CreateAsync(PickupRequest request)
    {
        await _requests.InsertOneAsync(request);
        return request;
    }

    public async Task<bool> UpdateAsync(string id, PickupRequest request)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return false;
        request.Id = id;
        var result = await _requests.ReplaceOneAsync(r => r.Id == id, request);
        return result.ModifiedCount > 0 || result.MatchedCount > 0;
    }
}
