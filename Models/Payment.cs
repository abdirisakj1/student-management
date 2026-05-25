using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWasteManagement.Models;

public class Payment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    [Required]
    public string CustomerId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? AdminId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? PickupRequestId { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [StringLength(50)]
    public string Currency { get; set; } = "USD";
    
    [StringLength(50)]
    public string Status { get; set; } = "Pending";
    
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ApprovedAt { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }

    // Legacy fields for backward compatibility
    public string? InvoiceNumber { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [StringLength(500)]
    public string? RequestReason { get; set; }
    
    public DateTime? PaidAt { get; set; }
    
    [StringLength(500)]
    public string? ApprovalNotes { get; set; }
}
