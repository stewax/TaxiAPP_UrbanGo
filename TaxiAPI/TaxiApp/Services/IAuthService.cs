using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiApp.Services
{
    public interface IAuthService
    {
        Task<AuthResult?> LoginAsync(string phone, string password);
        Task<AuthResult?> RegisterAsync(string fullName, string phone, string password);
        void SetToken(string token);
        string? GetToken();
        void ClearToken();
    }

    // Модели для авторизации
    public class AuthResult
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
    }
}
