namespace SmartWasteManagement.Data;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } =
        Environment.GetEnvironmentVariable("MONGODBCONN") ?? "";

    public string DatabaseName { get; set; } = "SmartWasteDb";

    public string UsersCollectionName { get; set; } = "Users";
    public string TrucksCollectionName { get; set; } = "Trucks";
    public string PlacesCollectionName { get; set; } = "Places";
    public string SmartBinsCollectionName { get; set; } = "SmartBins";
    public string RoutesCollectionName { get; set; } = "Routes";
    public string CollectionsCollectionName { get; set; } = "Collections";
    public string PickupRequestsCollectionName { get; set; } = "PickupRequests";
    public string NotificationsCollectionName { get; set; } = "Notifications";
    public string PaymentsCollectionName { get; set; } = "Payments";
    public string ComplaintsCollectionName { get; set; } = "Complaints";
    public string RefreshTokensCollectionName { get; set; } = "RefreshTokens";
}