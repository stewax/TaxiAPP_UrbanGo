using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TaxiApp.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private string? _token;
        private const string TokenFileName = "taxi_token.dat";

        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://localhost:7215/api/");
            LoadToken();
        }

        public async Task<AuthResult?> LoginAsync(string phone, string password)
        {
            try
            {
                var loginData = new { phone, password };
                var json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<LoginResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result != null)
                    {
                        SetToken(result.Token);
                        return new AuthResult
                        {
                            Token = result.Token,
                            Role = result.Role,
                            UserId = result.UserId,
                            Success = true,
                            Message = "Вход выполнен успешно"
                        };
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Неверный телефон или пароль"
                    };
                }

                return new AuthResult
                {
                    Success = false,
                    Message = "Ошибка сервера"
                };
            }
            catch (Exception ex)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = $"Ошибка подключения: {ex.Message}"
                };
            }
        }

        public async Task<AuthResult?> RegisterAsync(string fullName, string phone, string password)
        {
            try
            {
                var registerData = new
                {
                    fullName = fullName,
                    phone = phone,
                    password = password,
                };

                var json = JsonSerializer.Serialize(registerData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("auth/register", content);

                if (response.IsSuccessStatusCode)
                {
                    return new AuthResult
                    {
                        Success = true,
                        Message = "Регистрация успешна"
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new AuthResult
                    {
                        Success = false,
                        Message = $"Ошибка регистрации: {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = $"Ошибка подключения: {ex.Message}"
                };
            }
        }

        public void SetToken(string token)
        {
            _token = token;
            SaveToken(token);
        }

        public string? GetToken()
        {
            return _token;
        }

        public void ClearToken()
        {
            _token = null;
            DeleteToken();
        }

        private void SaveToken(string token)
        {
            try
            {
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TaxiApp");

                if (!Directory.Exists(appDataPath))
                    Directory.CreateDirectory(appDataPath);

                var tokenPath = Path.Combine(appDataPath, TokenFileName);

                // Простое шифрование токена
                var protectedToken = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(token),
                    null,
                    DataProtectionScope.CurrentUser);

                File.WriteAllBytes(tokenPath, protectedToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving token: {ex.Message}");
            }
        }

        private void LoadToken()
        {
            try
            {
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TaxiApp");

                var tokenPath = Path.Combine(appDataPath, TokenFileName);

                if (File.Exists(tokenPath))
                {
                    var protectedToken = File.ReadAllBytes(tokenPath);
                    var tokenBytes = ProtectedData.Unprotect(
                        protectedToken,
                        null,
                        DataProtectionScope.CurrentUser);

                    _token = Encoding.UTF8.GetString(tokenBytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading token: {ex.Message}");
            }
        }

        private void DeleteToken()
        {
            try
            {
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TaxiApp");

                var tokenPath = Path.Combine(appDataPath, TokenFileName);

                if (File.Exists(tokenPath))
                    File.Delete(tokenPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting token: {ex.Message}");
            }
        }
    }

    // Вспомогательные классы
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
}
