using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWasteManagement.Models;

public class RoutePlan
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? AssignedDriverId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? AssignedTruckId { get; set; }

    public List<string> StopLocations { get; set; } = new();

    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    public int ProgressPercent { get; set; }

    public DateTime ScheduledDate { get; set; } = DateTime.UtcNow.Date;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
