using System;
using System.ComponentModel.DataAnnotations;

namespace Tourism_Management.Models
{
    public class TourismModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string ? Title { get; set; }  

        [Required]
        public string ? Description { get; set; }

        [Required]
        [StringLength(100)]
        public string ? Location { get; set; }

        [Required]
        public decimal ? Price { get; set; }

        public int DurationDays  { get; set; } 

        public string ? ImageUrl  { get; set; }

        public bool IsAvailable { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
    }
