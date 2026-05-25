using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWasteManagement.Models;

public class Complaint
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    [Required]
    public string CustomerId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? DriverId { get; set; }

    [StringLength(80)]
    public string Category { get; set; } = "General";

    [StringLength(140)]
    [Required]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    [Required]
    public string Description { get; set; } = string.Empty;

    [StringLength(40)]
    public string Status { get; set; } = "Open";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

