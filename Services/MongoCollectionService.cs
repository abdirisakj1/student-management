using MongoDB.Driver;
using SmartWasteManagement.Data;
using Microsoft.Extensions.Options;

namespace SmartWasteManagement.Services;

public interface IMongoCollectionService<T>
{
    IMongoCollection<T> Collection { get; }
}

public class MongoCollectionService<T> : IMongoCollectionService<T>
{
    public IMongoCollection<T> Collection { get; }

    public MongoCollectionService(IMongoDatabase database, string collectionName)
    {
        Collection = database.GetCollection<T>(collectionName);
    }
}

public static class MongoServiceRegistration
{
    public static IServiceCollection AddMongoCollection<T>(
        this IServiceCollection services,
        Func<MongoDbSettings, string> collectionNameSelector)
    {
        services.AddSingleton<IMongoCollectionService<T>>(sp =>
        {
            var db = sp.GetRequiredService<IMongoDatabase>();
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbSettings>>().Value;
            return new MongoCollectionService<T>(db, collectionNameSelector(settings));
        });
        return services;
    }
}
