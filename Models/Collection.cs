using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWasteManagement.Models;

public class Collection
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? DriverId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    [Required]
    public string CustomerId { get; set; } = string.Empty;

    [StringLength(500)]
    [Required]
    public string Address { get; set; } = string.Empty;

    [BsonElement("wasteWeight")]
    public decimal WasteWeight { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}
