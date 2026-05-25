using MongoDB.Driver;
using SmartWasteManagement.Data;
using SmartWasteManagement.Models;
using Microsoft.Extensions.Options;

namespace SmartWasteManagement.Services;

public interface IPaymentService
{
    Task<List<Payment>> GetAllAsync();
    Task<List<Payment>> GetByCustomerIdAsync(string customerId);
    Task<List<Payment>> GetPendingAsync();
    Task<Payment?> GetByIdAsync(string id);
    Task<Payment> CreateAsync(Payment payment);
    Task<Payment> AdminChargeAsync(string customerId, string adminId, decimal amount, string? description, string? pickupRequestId = null);
    Task<bool> CustomerPayAsync(string id, string customerId);
    Task<bool> CustomerDeclineAsync(string id, string customerId);
    Task<bool> AdminApproveAsync(string id, string adminId, string? notes);
    Task<bool> AdminDeclineAsync(string id, string adminId, string? notes);
}

public class PaymentService : IPaymentService
{
    private readonly IMongoCollection<Payment> _payments;

    public PaymentService(IMongoDatabase database, IOptions<MongoDbSettings> options)
    {
        _payments = database.GetCollection<Payment>(options.Value.PaymentsCollectionName);
    }

    public async Task<List<Payment>> GetAllAsync() =>
        await _payments.Find(_ => true).SortByDescending(p => p.RequestedAt).ToListAsync();

    public async Task<List<Payment>> GetByCustomerIdAsync(string customerId) =>
        await _payments.Find(p => p.CustomerId == customerId)
            .SortByDescending(p => p.RequestedAt).ToListAsync();

    public async Task<List<Payment>> GetPendingAsync() =>
        await _payments.Find(p => p.Status == "Pending" || p.Status == "AwaitingAdmin")
            .SortByDescending(p => p.RequestedAt).ToListAsync();

    public async Task<Payment?> GetByIdAsync(string id)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return null;
        return await _payments.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Payment> CreateAsync(Payment payment)
    {
        await _payments.InsertOneAsync(payment);
        return payment;
    }

    public async Task<Payment> AdminChargeAsync(string customerId, string adminId, decimal amount, string? description, string? pickupRequestId = null)
    {
        var payment = new Payment
        {
            CustomerId = customerId,
            AdminId = adminId,
            PickupRequestId = pickupRequestId,
            Amount = amount,
            Description = description ?? "Waste collection service charge",
            Status = "Pending",
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            RequestedAt = DateTime.UtcNow
        };
        await _payments.InsertOneAsync(payment);
        return payment;
    }

    public async Task<bool> CustomerPayAsync(string id, string customerId)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return false;

        var update = Builders<Payment>.Update
            .Set(p => p.Status, "AwaitingAdmin")
            .Set(p => p.PaidAt, DateTime.UtcNow);

        var result = await _payments.UpdateOneAsync(
            p => p.Id == id && p.CustomerId == customerId && p.Status == "Pending", update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> CustomerDeclineAsync(string id, string customerId)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return false;

        var update = Builders<Payment>.Update.Set(p => p.Status, "DeclinedByCustomer");
        var result = await _payments.UpdateOneAsync(
            p => p.Id == id && p.CustomerId == customerId && p.Status == "Pending", update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> AdminApproveAsync(string id, string adminId, string? notes)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return false;

        var update = Builders<Payment>.Update
            .Set(p => p.Status, "Paid")
            .Set(p => p.AdminId, adminId)
            .Set(p => p.ApprovedAt, DateTime.UtcNow)
            .Set(p => p.PaidAt, DateTime.UtcNow)
            .Set(p => p.ApprovalNotes, notes);

        var result = await _payments.UpdateOneAsync(
            p => p.Id == id && (p.Status == "AwaitingAdmin" || p.Status == "Pending"), update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> AdminDeclineAsync(string id, string adminId, string? notes)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return false;

        var update = Builders<Payment>.Update
            .Set(p => p.Status, "Declined")
            .Set(p => p.AdminId, adminId)
            .Set(p => p.ApprovedAt, DateTime.UtcNow)
            .Set(p => p.ApprovalNotes, notes);

        var result = await _payments.UpdateOneAsync(p => p.Id == id, update);
        return result.ModifiedCount > 0;
    }
}
