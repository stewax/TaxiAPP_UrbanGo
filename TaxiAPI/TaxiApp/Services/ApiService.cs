using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using TaxiAPI.Models;
using System.Threading.Tasks;
using TaxiAPI.DTOs;
using TaxiApp.Models;

namespace TaxiApp.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly string _baseUrl;

        public ApiService(HttpClient httpClient, IAuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
            _baseUrl = "https://localhost:7215/api/"; // Порт вашего API
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<ClientProfile?> GetMyProfileAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(" [API] Начинаю запрос GET users/me");

                await AddAuthHeader();

                var response = await _httpClient.GetAsync("users/me");

                System.Diagnostics.Debug.WriteLine($"📥 [API] Статус ответа: {response.StatusCode}");

                // Читаем тело ответа в любом случае (даже при ошибке)
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"📄 [API] Тело ответа: {content}");

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ [API] Запрос неуспешен. Код: {response.StatusCode}");
                    return null;
                }

                // Пытаемся десериализовать
                try
                {
                    var profile = JsonSerializer.Deserialize<ClientProfile>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    System.Diagnostics.Debug.WriteLine($"✅ [API] Десериализация успешна");
                    return profile;
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ [API] Ошибка десериализации JSON: {ex.Message}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 [API] Критическое исключение: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        public async Task<bool> UpdateMyProfileAsync(ProfileUpdateDto dto)
        {
            try
            {
                await AddAuthHeader();
                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync("users/me/profile", content);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _authService.ClearToken();
                    throw new UnauthorizedAccessException("Токен истек");
                }

                return response.IsSuccessStatusCode;
            }
            catch (UnauthorizedAccessException) { throw; }
            catch { return false; }
        }

        public async Task<List<Order>?> GetMyOrdersAsync(int userId)
        {
            try
            {
                await AddAuthHeader();

                // Передаем userId как query-параметр
                var response = await _httpClient.GetAsync($"orders/my-orders?userId={userId}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _authService.ClearToken();
                    throw new UnauthorizedAccessException("Токен истек");
                }

                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<List<Order>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (UnauthorizedAccessException) { throw; }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ GetMyOrdersAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<Order?> CreateOrderAsync(OrderCreateDto dto)
        {
            try
            {
                await AddAuthHeader(); // Обязательно добавляем токен!

                var json = JsonSerializer.Serialize(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("orders", content);

                System.Diagnostics.Debug.WriteLine($"📥 Ответ сервера: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"📄 Тело ответа: {await response.Content.ReadAsStringAsync()}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _authService.ClearToken();
                    throw new UnauthorizedAccessException("Токен истек");
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ Ошибка API ({response.StatusCode}): {errorBody}");
                    return null;
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Order>(resultJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Пробрасываем дальше
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Исключение в CreateOrderAsync: {ex}");
                return null;
            }
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                await AddAuthHeader();
                var response = await _httpClient.GetAsync(endpoint);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _authService.ClearToken();
                    throw new UnauthorizedAccessException("Сессия истекла. Выполните вход заново.");
                }

                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GET Error ({endpoint}): {ex.Message}");
                throw;
            }
        }

        public async Task<T?> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                await AddAuthHeader();
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _authService.ClearToken();
                    throw new UnauthorizedAccessException("Сессия истекла. Выполните вход заново.");
                }

                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"POST Error ({endpoint}): {ex.Message}");
                throw;
            }
        }

        public async Task<T?> PutAsync<T>(string endpoint, int id, object data)
        {
            try
            {
                await AddAuthHeader();
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{endpoint}/{id}", content);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _authService.ClearToken();
                    throw new UnauthorizedAccessException("Сессия истекла. Выполните вход заново.");
                }

                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PUT Error ({endpoint}/{id}): {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string endpoint, int id)
        {
            try
            {
                await AddAuthHeader();
                var response = await _httpClient.DeleteAsync($"{endpoint}/{id}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _authService.ClearToken();
                    throw new UnauthorizedAccessException("Сессия истекла. Выполните вход заново.");
                }

                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DELETE Error ({endpoint}/{id}): {ex.Message}");
                throw;
            }
        }

        public async Task<byte[]?> DownloadFileAsync(string endpoint)
        {
            try
            {
                await AddAuthHeader();
                var response = await _httpClient.GetAsync(endpoint);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _authService.ClearToken();
                    throw new UnauthorizedAccessException("Сессия истекла. Выполните вход заново.");
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Download Error ({endpoint}): {ex.Message}");
                throw;
            }
        }

        private async Task AddAuthHeader()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;

            var token = _authService.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                System.Diagnostics.Debug.WriteLine($"🔑 Токен добавлен: {token.Substring(0, Math.Min(20, token.Length))}...");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ Токен НЕ найден в AuthService!");
            }

            await Task.CompletedTask;
        }
        public async Task<List<Tariff>?> GetTariffsAsync()
        {
            try
            {
                await AddAuthHeader();
                var response = await _httpClient.GetAsync("tariffs");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _authService.ClearToken();
                    throw new UnauthorizedAccessException("Сессия истекла.");
                }

                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<List<Tariff>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки тарифов: {ex.Message}");
                return null;
            }
        }
    }
}
