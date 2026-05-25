using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartWasteManagement.Data;
using SmartWasteManagement.Models;

namespace SmartWasteManagement.Services;

public interface IComplaintService
{
    Task<List<Complaint>> GetAllAsync();
    Task<List<Complaint>> GetByCustomerIdAsync(string customerId);
    Task<Complaint> CreateAsync(Complaint complaint);
    Task<bool> UpdateStatusAsync(string id, string status);
}

public class ComplaintService : IComplaintService
{
    private readonly IMongoCollection<Complaint> _complaints;

    public ComplaintService(IMongoDatabase database, IOptions<MongoDbSettings> options)
    {
        _complaints = database.GetCollection<Complaint>(options.Value.ComplaintsCollectionName);
    }

    public async Task<List<Complaint>> GetAllAsync() =>
        await _complaints.Find(_ => true).SortByDescending(c => c.CreatedAt).ToListAsync();

    public async Task<List<Complaint>> GetByCustomerIdAsync(string customerId) =>
        await _complaints.Find(c => c.CustomerId == customerId).SortByDescending(c => c.CreatedAt).ToListAsync();

    public async Task<Complaint> CreateAsync(Complaint complaint)
    {
        complaint.Id = null;
        complaint.CreatedAt = DateTime.UtcNow;
        complaint.Status = string.IsNullOrWhiteSpace(complaint.Status) ? "Pending" : complaint.Status;
        await _complaints.InsertOneAsync(complaint);
        return complaint;
    }

    public async Task<bool> UpdateStatusAsync(string id, string status)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return false;
        var update = Builders<Complaint>.Update.Set(c => c.Status, status);
        var result = await _complaints.UpdateOneAsync(c => c.Id == id, update);
        return result.ModifiedCount > 0;
    }
}

