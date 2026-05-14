using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWasteManagement.Models;

/// <summary>
/// Truck document stored in the Trucks collection.
/// </summary>
public class Truck
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [Required]
    [StringLength(50)]
    public string TruckNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string DriverName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Active";

    [Required]
    [StringLength(200)]
    public string Area { get; set; } = string.Empty;
}
