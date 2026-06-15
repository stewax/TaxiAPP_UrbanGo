using DocumentFormat.OpenXml.Bibliography;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TaxiAPI.Models
{
    [Table("orders")]
    public class Order
    {
        [Key, Column("id")]
        public int Id { get; set; }

        [Required, Column("code"), MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required, Column("client_id")]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        [InverseProperty("Orders")]
        public Client? Client { get; set; }

        [Column("driver_id")]
        public int? DriverId { get; set; }

        [ForeignKey("DriverId")]
        [InverseProperty("Orders")]
        public Driver? Driver { get; set; }

        [Required, Column("tariff_id")]
        public int TariffId { get; set; }

        [ForeignKey("TariffId")]
        public Tariff? Tariff { get; set; }

        [Required, Column("pickup_address")]
        public string PickupAddress { get; set; } = string.Empty;

        [Required, Column("destination_address")]
        public string DestinationAddress { get; set; } = string.Empty;

        [Required, Column("status"), MaxLength(20)]
        public string Status { get; set; } = "pending";

        [Required, Column("estimated_cost", TypeName = "decimal(10,2)")]
        public decimal EstimatedCost { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public Trip? Trip { get; set; }
    }
}
