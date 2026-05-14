using MongoDB.Bson;
using MongoDB.Driver;
using SmartWasteManagement.Data;
using SmartWasteManagement.Models;
using Microsoft.Extensions.Options;

namespace SmartWasteManagement.Services;

public interface IUserService
{
    Task<List<User>> GetAllAsync();
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task<bool> DeleteAsync(string id);
}

/// <summary>
/// MongoDB-backed user repository. Resolves <c>role</c> vs <c>Role</c> field names in BSON documents.
/// </summary>
public class UserService : IUserService
{
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<BsonDocument> _usersBson;

    public UserService(IMongoDatabase database, IOptions<MongoDbSettings> options)
    {
        var name = options.Value.UsersCollectionName;
        _users = database.GetCollection<User>(name);
        _usersBson = database.GetCollection<BsonDocument>(name);
    }

    public async Task<List<User>> GetAllAsync()
    {
        var list = await _users.Find(_ => true).ToListAsync();
        foreach (var u in list)
            await MergeRoleFromBsonAsync(u);
        return list;
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return null;
        var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user is not null)
            await MergeRoleFromBsonAsync(user);
        return user;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
        if (user is not null)
            await MergeRoleFromBsonAsync(user);
        return user;
    }

    public async Task<User> CreateAsync(User user)
    {
        await _users.InsertOneAsync(user);
        return user;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return false;
        var result = await _users.DeleteOneAsync(u => u.Id == id);
        return result.DeletedCount > 0;
    }

    /// <summary>
    /// Some documents use BSON <c>Role</c> (driver default) and others use <c>role</c> (Compass). Prefer explicit raw read when needed.
    /// </summary>
    private async Task MergeRoleFromBsonAsync(User user)
    {
        if (string.IsNullOrEmpty(user.Id) || !MongoDB.Bson.ObjectId.TryParse(user.Id, out var oid))
            return;

        var doc = await _usersBson.Find(Builders<BsonDocument>.Filter.Eq("_id", oid)).FirstOrDefaultAsync();
        if (doc is null)
            return;

        if (doc.TryGetValue("role", out var lower) && lower.IsString)
        {
            user.Role = lower.AsString;
            return;
        }

        if (doc.TryGetValue("Role", out var pascal) && pascal.IsString)
            user.Role = pascal.AsString;
    }
}
