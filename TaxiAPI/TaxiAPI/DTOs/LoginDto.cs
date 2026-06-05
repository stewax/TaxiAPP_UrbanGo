using System.ComponentModel.DataAnnotations;

namespace TaxiAPI.DTOs
{
    public class LoginDto
    {
        [Required] public string Phone { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }
}
