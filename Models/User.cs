using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWasteManagement.Models;

/// <summary>
/// User document stored in the Users collection.
/// </summary>
public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [Required]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// BCrypt password hash; never returned in JSON responses.
    /// </summary>
    [JsonIgnore]
    public string Password { get; set; } = string.Empty;

    /// <summary>Stored in MongoDB as lowercase <c>role</c> (Compass default) or <c>Role</c>.</summary>
    [BsonElement("role")]
    [Required]
    [StringLength(50)]
    public string Role { get; set; } = Roles.User;

    [Phone]
    [StringLength(30)]
    public string? Phone { get; set; }
}
