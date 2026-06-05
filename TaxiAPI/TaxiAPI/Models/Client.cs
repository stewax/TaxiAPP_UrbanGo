using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TaxiAPI.Models
{
    [Table("clients")]
    public class Client
    {
        [Key, Column("id")]
        public int Id { get; set; }

        [Required, Column("user_id")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required, Column("full_name"), MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, Column("code"), MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
