using MongoDB.Driver;
using Tourism_Management.Models;

namespace Tourism_Management.Services
{
    public interface ITourismUserService
    {
        Task<List<TourismUsers>> GetAsync();
        Task<TourismUsers?> GetByIdAsync(int id);
        Task<TourismUsers> CreateAsync(TourismUsers user);
        Task<bool> UpdateAsync(int id, TourismUsers updatedUser);
        Task<bool> DeleteAsync(int id);
    }

    public class TourismUserService : ITourismUserService
    {
        private readonly IMongoCollection<TourismUsers> _usersCollection;

        public TourismUserService(MongoSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _usersCollection = database.GetCollection<TourismUsers>(settings.UsersCollectionName);
        }

        public async Task<List<TourismUsers>> GetAsync() =>
            await _usersCollection.Find(_ => true).ToListAsync();

        public async Task<TourismUsers?> GetByIdAsync(int id) =>
            await _usersCollection.Find(u => u.Id == id).FirstOrDefaultAsync();

        public async Task<TourismUsers> CreateAsync(TourismUsers user)
        {
            var last = await _usersCollection.Find(_ => true).SortByDescending(u => u.Id).FirstOrDefaultAsync();
            user.Id = (last?.Id ?? 0) + 1;
            await _usersCollection.InsertOneAsync(user);
            return user;
        }

        public async Task<bool> UpdateAsync(int id, TourismUsers updatedUser)
        {
            var result = await _usersCollection.ReplaceOneAsync(u => u.Id == id, updatedUser);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _usersCollection.DeleteOneAsync(u => u.Id == id);
            return result.IsAcknowledged && result.DeletedCount > 0;
        }
    }
}