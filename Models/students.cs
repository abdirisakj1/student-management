using System;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Tourism_Management.Models
{
    public class TourismUsers
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? _id { get; set; }

        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? name { get; set; }

        [Required]
        [StringLength(100)]
        public string? faculty { get; set; }

        [Required]
        [StringLength(200)]
        public string? adress { get; set; }

        [Required]
        [StringLength(20)]
        public string? number { get; set; }
    }
}