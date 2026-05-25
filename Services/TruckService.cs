using MongoDB.Driver;
using SmartWasteManagement.Data;
using SmartWasteManagement.Models;
using Microsoft.Extensions.Options;

namespace SmartWasteManagement.Services;

public interface ITruckService
{
    Task<List<Truck>> GetAllAsync();
    Task<List<Truck>> GetByStatusAsync(string status);
    Task<Truck?> GetByIdAsync(string id);
    Task<Truck?> GetByDriverIdAsync(string driverId);
    Task<Truck> CreateAsync(Truck truck);
    Task<bool> UpdateAsync(string id, Truck truck);
    Task<bool> DeleteAsync(string id);
    Task<bool> AssignDriverAsync(string truckId, string driverId, string? driverName = null);
    Task<bool> UnassignDriverAsync(string truckId);
    Task<bool> UpdateStatusAsync(string truckId, string status);
}

/// <summary>
/// MongoDB-backed truck repository with driver assignment and status management.
/// </summary>
public class TruckService : ITruckService
{
    private readonly IMongoCollection<Truck> _trucks;
    private readonly IUserService _users;

    public TruckService(IMongoDatabase database, IOptions<MongoDbSettings> options, IUserService users)
    {
        _trucks = database.GetCollection<Truck>(options.Value.TrucksCollectionName);
        _users = users;
    }

    public async Task<List<Truck>> GetAllAsync() =>
        await _trucks.Find(_ => true).ToListAsync();

    public async Task<List<Truck>> GetByStatusAsync(string status) =>
        await _trucks.Find(t => t.Status == status).ToListAsync();

    public async Task<Truck?> GetByIdAsync(string id)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return null;
        return await _trucks.Find(t => t.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Truck?> GetByDriverIdAsync(string driverId)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(driverId, out _))
            return null;
        return await _trucks.Find(t => t.DriverId == driverId).FirstOrDefaultAsync();
    }

    public async Task<Truck> CreateAsync(Truck truck)
    {
        if (string.IsNullOrWhiteSpace(truck.TruckNumber))
        {
            var existing = await GetAllAsync();
            truck.TruckNumber = FleetCodeGenerator.GenerateTruckNumber(existing);
        }

        truck.UpdatedAt = DateTime.UtcNow;
        await _trucks.InsertOneAsync(truck);
        return truck;
    }

    public async Task<bool> UpdateAsync(string id, Truck truck)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return false;
        truck.Id = id;
        truck.UpdatedAt = DateTime.UtcNow;
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

    public async Task<bool> AssignDriverAsync(string truckId, string driverId, string? driverName = null)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(truckId, out _) ||
            !MongoDB.Bson.ObjectId.TryParse(driverId, out _))
            return false;

        var truck = await GetByIdAsync(truckId);
        if (truck is null)
            return false;

        var driver = await _users.GetByIdAsync(driverId);
        if (driver is null)
            return false;

        var resolvedName = !string.IsNullOrWhiteSpace(driverName)
            ? driverName
            : driver.FullName;

        // Remove this driver from any other truck they were assigned to
        var otherTrucks = await _trucks.Find(t => t.DriverId == driverId && t.Id != truckId).ToListAsync();
        foreach (var other in otherTrucks)
        {
            await _trucks.UpdateOneAsync(
                t => t.Id == other.Id,
                Builders<Truck>.Update
                    .Set(t => t.DriverId, (string?)null)
                    .Set(t => t.DriverName, (string?)null)
                    .Set(t => t.UpdatedAt, DateTime.UtcNow));
        }

        // Clear previous driver on this truck
        if (!string.IsNullOrEmpty(truck.DriverId) && truck.DriverId != driverId)
        {
            var previousDriver = await _users.GetByIdAsync(truck.DriverId);
            if (previousDriver is not null)
            {
                previousDriver.AssignedTruckId = null;
                await _users.UpdateAsync(truck.DriverId, previousDriver);
            }
        }

        var truckUpdate = Builders<Truck>.Update
            .Set(t => t.DriverId, driverId)
            .Set(t => t.DriverName, resolvedName)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);

        var result = await _trucks.UpdateOneAsync(t => t.Id == truckId, truckUpdate);
        if (result.MatchedCount == 0)
            return false;

        driver.AssignedTruckId = truckId;
        await _users.UpdateAsync(driverId, driver);

        return true;
    }

    public async Task<bool> UnassignDriverAsync(string truckId)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(truckId, out _))
            return false;

        var truck = await GetByIdAsync(truckId);
        if (truck is null)
            return false;

        if (!string.IsNullOrEmpty(truck.DriverId))
        {
            var driver = await _users.GetByIdAsync(truck.DriverId);
            if (driver is not null)
            {
                driver.AssignedTruckId = null;
                await _users.UpdateAsync(truck.DriverId, driver);
            }
        }

        var update = Builders<Truck>.Update
            .Set(t => t.DriverId, (string?)null)
            .Set(t => t.DriverName, (string?)null)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);

        var result = await _trucks.UpdateOneAsync(t => t.Id == truckId, update);
        return result.MatchedCount > 0;
    }

    public async Task<bool> UpdateStatusAsync(string truckId, string status)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(truckId, out _))
            return false;

        var update = Builders<Truck>.Update
            .Set(t => t.Status, status)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);

        var result = await _trucks.UpdateOneAsync(t => t.Id == truckId, update);
        return result.ModifiedCount > 0;
    }
}
