namespace Tourism_Management.Models
{
    public class MongoSettings
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "Students";
        public string UsersCollectionName { get; set; } = "Users";
    }
}