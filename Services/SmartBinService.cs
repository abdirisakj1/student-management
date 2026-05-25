using MongoDB.Driver;
using SmartWasteManagement.Data;
using SmartWasteManagement.Models;
using Microsoft.Extensions.Options;

namespace SmartWasteManagement.Services;

public interface ISmartBinService
{
    Task<List<SmartBin>> GetAllAsync();
    Task<SmartBin?> GetByIdAsync(string id);
    Task<SmartBin> CreateAsync(SmartBin bin);
    Task<bool> UpdateAsync(string id, SmartBin bin);
    Task<bool> DeleteAsync(string id);
}

public class SmartBinService : ISmartBinService
{
    private readonly IMongoCollection<SmartBin> _bins;

    public SmartBinService(IMongoDatabase database, IOptions<MongoDbSettings> options)
    {
        _bins = database.GetCollection<SmartBin>(options.Value.SmartBinsCollectionName);
    }

    public async Task<List<SmartBin>> GetAllAsync() =>
        await _bins.Find(_ => true).ToListAsync();

    public async Task<SmartBin?> GetByIdAsync(string id)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return null;
        return await _bins.Find(b => b.Id == id).FirstOrDefaultAsync();
    }

    public async Task<SmartBin> CreateAsync(SmartBin bin)
    {
        if (string.IsNullOrWhiteSpace(bin.BinCode))
        {
            var existing = await GetAllAsync();
            bin.BinCode = FleetCodeGenerator.GenerateBinCode(existing, bin.Location);
        }

        bin.LastUpdated = DateTime.UtcNow;
        bin.IsOverflow = bin.FillLevelPercent >= 90;
        await _bins.InsertOneAsync(bin);
        return bin;
    }

    public async Task<bool> UpdateAsync(string id, SmartBin bin)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return false;
        bin.Id = id;
        bin.LastUpdated = DateTime.UtcNow;
        bin.IsOverflow = bin.FillLevelPercent >= 90;
        var result = await _bins.ReplaceOneAsync(b => b.Id == id, bin);
        return result.ModifiedCount > 0 || result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return false;
        var result = await _bins.DeleteOneAsync(b => b.Id == id);
        return result.DeletedCount > 0;
    }
}
