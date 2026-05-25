using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWasteManagement.Models;

public class Truck
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [StringLength(50)]
    public string TruckNumber { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? DriverId { get; set; }

    [StringLength(200)]
    public string? DriverName { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Active";

    [Required]
    [StringLength(200)]
    public string Area { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Model { get; set; }

    [BsonElement("capacity")]
    public int CapacityKg { get; set; } = 5000;

    public DateTime? LastMaintenance { get; set; }

    public DateTime? NextMaintenance { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
