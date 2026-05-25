using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWasteManagement.Models;

public class SmartBin
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [StringLength(100)]
    public string BinCode { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string Location { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    [Range(0, 100)]
    public int FillLevelPercent { get; set; }

    public bool IsOverflow { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Normal";

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
