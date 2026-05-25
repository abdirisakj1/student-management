using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWasteManagement.Models;

public class PickupRequest
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string CustomerId { get; set; } = string.Empty;

    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [StringLength(50)]
    public string WasteType { get; set; } = "General";

    public DateTime PreferredDate { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    [StringLength(500)]
    public string? Notes { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? PaymentId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? AssignedDriverId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
