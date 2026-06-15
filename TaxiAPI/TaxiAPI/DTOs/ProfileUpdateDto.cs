using System.ComponentModel.DataAnnotations;

namespace TaxiAPI.DTOs
{
    public class ProfileUpdateDto
    {
        [Required(ErrorMessage = "ФИО обязательно для заполнения")]
        [MaxLength(100, ErrorMessage = "ФИО не должно превышать 100 символов")]
        public string FullName { get; set; } = string.Empty;
    }
}
