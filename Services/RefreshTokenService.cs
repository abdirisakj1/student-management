using System.Security.Cryptography;
using MongoDB.Driver;
using SmartWasteManagement.Data;
using SmartWasteManagement.Models;
using Microsoft.Extensions.Options;

namespace SmartWasteManagement.Services;

public interface IRefreshTokenService
{
    Task<(string Token, DateTime ExpiresAt)> CreateAsync(string userId);
    Task<RefreshToken?> GetValidAsync(string token);
    Task RevokeAsync(string id);
}

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IMongoCollection<RefreshToken> _tokens;

    public RefreshTokenService(IMongoDatabase database, IOptions<MongoDbSettings> options)
    {
        _tokens = database.GetCollection<RefreshToken>(options.Value.RefreshTokensCollectionName);
    }

    public async Task<(string Token, DateTime ExpiresAt)> CreateAsync(string userId)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expires = DateTime.UtcNow.AddDays(7);
        await _tokens.InsertOneAsync(new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expires
        });
        return (token, expires);
    }

    public async Task<RefreshToken?> GetValidAsync(string token)
    {
        var stored = await _tokens.Find(t => t.Token == token && !t.IsRevoked).FirstOrDefaultAsync();
        if (stored is null || stored.ExpiresAt < DateTime.UtcNow)
            return null;
        return stored;
    }

    public async Task RevokeAsync(string id)
    {
        if (!MongoDB.Bson.ObjectId.TryParse(id, out _))
            return;
        await _tokens.UpdateOneAsync(
            t => t.Id == id,
            Builders<RefreshToken>.Update.Set(t => t.IsRevoked, true));
    }
}
