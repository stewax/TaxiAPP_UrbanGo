using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TaxiApp.Services;
using TaxiApp.ViewModels;

namespace TaxiApp.Views.Pages;

public partial class ProfilePage : UserControl
{
    private readonly MainViewModel _vm;
    private readonly IAuthService _authService;

    public ProfilePage(MainViewModel vm)
    {
        _vm = vm;

        var app = (App)Application.Current;
        _authService = (IAuthService)app.Services.GetService(typeof(IAuthService))!;

        InitializeComponent();
        Loaded += ProfilePage_Loaded;
    }

    private async void ProfilePage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadProfile();
    }

    // 🔥 Загрузка профиля через /me
    private async Task LoadProfile()
    {
        try
        {
            var token = _authService.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                MessageBox.Show("Сессия истекла. Войдите снова.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            System.Diagnostics.Debug.WriteLine($"🔑 Загрузка профиля через /me");

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            using var client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://localhost:7215/api/");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // 🔥 Используем /me вместо /clients/{id}
            var response = await client.GetAsync("clients/me");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"📥 Профиль получен: {json}");

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("fullName", out var fullName))
                    FullNameInput.Text = fullName.GetString() ?? "";

                if (root.TryGetProperty("user", out var user) &&
                    user.TryGetProperty("phone", out var phone))
                    PhoneInput.Text = phone.GetString() ?? "";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _authService.ClearToken();
                MessageBox.Show("Сессия истекла. Войдите снова.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"⚠️ Статус: {response.StatusCode} — {error}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки: {ex.Message}");
        }
    }

    // 🔥 Сохранение профиля через /me
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var token = _authService.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                MessageBox.Show("Сессия истекла. Войдите снова.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            System.Diagnostics.Debug.WriteLine($"📤 [ProfilePage] Отправка обновления: {FullNameInput.Text}");

            var updateData = new { fullName = FullNameInput.Text };
            var json = JsonSerializer.Serialize(updateData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            using var client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://localhost:7215/api/");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // 🔥 Используем /me вместо /clients/{id}
            var response = await client.PutAsync("clients/me", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine($"📥 Ответ: {response.StatusCode} — {responseBody}");

            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("✅ Профиль успешно обновлен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _authService.ClearToken();
                MessageBox.Show("Сессия истекла. Войдите снова.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show($"Ошибка: {response.StatusCode}\n{responseBody}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Ошибка: {ex.Message}");
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}