namespace SmartWasteManagement.Data;

/// <summary>
/// MongoDB connection and collection names for Smart Waste Management.
/// </summary>
public class MongoDbSettings
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "SmartWasteDb";
    public string UsersCollectionName { get; set; } = "Users";
    public string TrucksCollectionName { get; set; } = "Trucks";
    public string PlacesCollectionName { get; set; } = "Places";
}
