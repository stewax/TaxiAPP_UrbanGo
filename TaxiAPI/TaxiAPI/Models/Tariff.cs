using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TaxiAPI.Models
{
    [Table("tariffs")]
    public class Tariff
    {
        [Key, Column("id")]
        public int Id { get; set; }

        [Required, Column("name"), MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required, Column("base_price", TypeName = "decimal(10,2)")]
        public decimal BasePrice { get; set; }

        [Required, Column("price_per_km", TypeName = "decimal(10,2)")]
        public decimal PricePerKm { get; set; }

        [Required, Column("min_price", TypeName = "decimal(10,2)")]
        public decimal MinPrice { get; set; }

        [Required, Column("status"), MaxLength(20)]
        public string Status { get; set; } = "active";

        [Required, Column("code"), MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
