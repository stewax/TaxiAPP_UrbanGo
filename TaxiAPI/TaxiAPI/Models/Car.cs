using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TaxiAPI.Models
{
    [Index(nameof(LicensePlate), IsUnique = true)]
    [Index(nameof(Code), IsUnique = true)]
    [Table("cars")]
    public class Car
    {
        [Key, Column("id")]
        public int Id { get; set; }

        [Required, Column("brand"), MaxLength(50)]
        public string Brand { get; set; } = string.Empty;

        [Required, Column("model"), MaxLength(50)]
        public string Model { get; set; } = string.Empty;

        [Required, Column("license_plate"), MaxLength(20)]
        public string LicensePlate { get; set; } = string.Empty;

        [Required, Column("production_date")]
        public DateOnly ProductionDate { get; set; }

        [Required, Column("code"), MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Column("driver_id")]
        public int? DriverId { get; set; }

        [ForeignKey("DriverId")]
        public Driver? Driver { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
