using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaxiAPI.Models
{
    [Table("users")]
    public class User
    {
        [Key, Column("id")]
        public int Id { get; set; }

        [Required, Column("phone"), MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required, Column("password_hash"), MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, Column("role"), MaxLength(20)]
        public string Role { get; set; } = "client";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public Client? Client { get; set; }
    }
}
