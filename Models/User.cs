using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWasteManagement.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [Required]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Username { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [JsonIgnore]
    public string Password { get; set; } = string.Empty;

    [BsonElement("role")]
    [Required]
    [StringLength(50)]
    public string Role { get; set; } = Roles.Customer;

    [Phone]
    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? LicenseNumber { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? AssignedTruckId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? AssignedRouteId { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Active";

    public bool IsActive { get; set; } = true;

    [StringLength(500000)]
    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
