using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWasteManagement.Models;

public class CollectionRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? RouteId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? DriverId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? TruckId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? PickupRequestId { get; set; }

    [StringLength(300)]
    public string Location { get; set; } = string.Empty;

    [StringLength(50)]
    public string Status { get; set; } = "Completed";

    public double WeightKg { get; set; }

    [StringLength(500)]
    public string? ProofPhotoUrl { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}
