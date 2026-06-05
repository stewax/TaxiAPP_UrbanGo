using System.ComponentModel.DataAnnotations;

namespace TaxiAPI.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "ФИО обязательно для заполнения")]
        [MinLength(2, ErrorMessage = "ФИО должно содержать не менее 2 символов")]
        [RegularExpression(@"^[a-zA-Zа-яА-ЯёЁ\s\-]+$",
            ErrorMessage = "ФИО может содержать только буквы, пробелы и дефисы")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Номер телефона обязателен")]
        [RegularExpression(@"^\+?\d{10,15}$",
            ErrorMessage = "Введите корректный номер телефона (10-15 цифр)")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов")]
        public string Password { get; set; } = string.Empty;

        
        
    }
}
