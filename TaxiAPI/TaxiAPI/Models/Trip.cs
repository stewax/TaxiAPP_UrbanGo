using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TaxiAPI.Models
{
    [Table("trips")]
    public class Trip
    {
        [Key, Column("id")]
        public int Id { get; set; }

        [Required, Column("order_id")]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        [Required, Column("distance_km", TypeName = "decimal(10,2)")]
        public decimal DistanceKm { get; set; }

        [Required, Column("duration_minutes")]
        public int DurationMinutes { get; set; }

        [Required, Column("start_time")]
        public DateTime StartTime { get; set; }

        [Required, Column("end_time")]
        public DateTime EndTime { get; set; }

        [Required, Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required, Column("code"), MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
