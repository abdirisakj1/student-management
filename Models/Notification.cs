using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWasteManagement.Models;

public class Notification
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    [Required]
    public string UserId { get; set; } = string.Empty;

    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    [StringLength(50)]
    public string Type { get; set; } = "Info";

    /// <summary>
    /// Optional UI navigation target for the frontend (e.g. "/admin/payments").
    /// </summary>
    [StringLength(300)]
    public string? ActionUrl { get; set; }

    [StringLength(80)]
    public string? ActionText { get; set; }

    [StringLength(300)]
    public string? SecondaryActionUrl { get; set; }

    [StringLength(80)]
    public string? SecondaryActionText { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? ReferenceId { get; set; }

    [StringLength(50)]
    public string? ReferenceType { get; set; }

    public bool PrimaryActionDisabled { get; set; }

    public bool HidePrimaryAction { get; set; }

    public bool HideSecondaryAction { get; set; }

    [BsonElement("readStatus")]
    public bool ReadStatus { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Legacy field for backward compatibility
    public string? Title { get; set; }

    [BsonIgnore]
    [Obsolete("Use ReadStatus instead")]
    public bool IsRead
    {
        get => ReadStatus;
        set => ReadStatus = value;
    }
}
