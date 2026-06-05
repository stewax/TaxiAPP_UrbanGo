using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
                    new AuthenticationHeaderValue("Bearer", token);
            }

            await Task.CompletedTask;
        }
    }
}
