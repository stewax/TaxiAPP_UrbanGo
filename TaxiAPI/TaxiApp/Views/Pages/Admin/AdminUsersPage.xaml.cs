using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TaxiApp.Models;
using TaxiApp.Services;
using TaxiApp.ViewModels;

namespace TaxiApp.Views.Pages.Admin;

public partial class AdminUsersPage : UserControl
{
    private readonly MainViewModel _vm;
    private readonly IAuthService _authService;
    private List<AdminUserDto> _users = new();

    public AdminUsersPage(MainViewModel vm)
    {
        _vm = vm;
        var app = (App)Application.Current;
        _authService = app.Services.GetRequiredService<IAuthService>();
        InitializeComponent();
        Loaded += async (s, e) => await LoadUsers();
    }

    private async Task LoadUsers()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🔄 Загрузка пользователей...");

            var token = _authService.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                System.Diagnostics.Debug.WriteLine("❌ Токен не найден");
                return;
            }

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (s, c, ch, e) => true
            };

            using var client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://localhost:7215/api/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            System.Diagnostics.Debug.WriteLine($"📤 Запрос GET api/users");

            var response = await client.GetAsync("users");

            System.Diagnostics.Debug.WriteLine($"📥 Ответ: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"📄 JSON: {json}");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                _users = JsonSerializer.Deserialize<List<AdminUserDto>>(json, options) ?? new();

                System.Diagnostics.Debug.WriteLine($"✅ Загружено {_users.Count} пользователей");

                UsersGrid.ItemsSource = _users;

                if (_users.Count == 0)
                {
                    MessageBox.Show("Пользователи не найдены в базе данных", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                System.Diagnostics.Debug.WriteLine("❌ Endpoint не найден (404)");
                MessageBox.Show("API endpoint /api/users не найден. Проверьте контроллер.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                System.Diagnostics.Debug.WriteLine("❌ Доступ запрещен (403) - проверьте роль admin");
                MessageBox.Show("Недостаточно прав. Убедитесь, что у пользователя роль 'admin'.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка {response.StatusCode}: {error}");
                MessageBox.Show($"Ошибка: {response.StatusCode}\n{error}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"💥 Исключение: {ex.Message}\n{ex.StackTrace}");
            MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadUsers();
    }

    private void ViewUser_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int id)
        {
            var user = _users.Find(u => u.Id == id);
            if (user != null)
            {
                var info = $"👤 Информация о пользователе\n\n" +
                          $"ID: {user.Id}\n" +
                          $"📱 Телефон: {user.Phone}\n" +
                          $"🎭 Роль: {user.Role}\n" +
                          $"📅 Регистрация: {user.CreatedAt:dd.MM.yyyy HH:mm}\n\n";

                if (!string.IsNullOrEmpty(user.ClientName))
                    info += $"👤 Имя клиента: {user.ClientName}\n🔖 Код: {user.ClientCode}\n\n";

                if (!string.IsNullOrEmpty(user.DriverName))
                    info += $"🚗 Имя водителя: {user.DriverName}\n📊 Статус: {user.DriverStatus}\n";

                MessageBox.Show(info, "Информация о пользователе", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private async void ChangeRole_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int id)
        {
            var user = _users.Find(u => u.Id == id);
            if (user == null) return;

            var window = new Window
            {
                Title = "Смена роли",
                Width = 350,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock { Text = $"Пользователь: {user.Phone}", Margin = new Thickness(0, 0, 0, 10) });
            panel.Children.Add(new TextBlock { Text = "Выберите новую роль:", Margin = new Thickness(0, 0, 0, 10) });

            var combo = new ComboBox();
            combo.Items.Add("client");
            combo.Items.Add("driver");
            combo.Items.Add("admin");
            combo.SelectedItem = user.Role;
            panel.Children.Add(combo);

            var saveBtn = new Button { Content = "Сохранить", Margin = new Thickness(0, 15, 0, 0), Padding = new Thickness(10, 5, 10, 5) };
            saveBtn.Click += async (s, ev) =>
            {
                var newRole = combo.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(newRole)) return;

                try
                {
                    var token = _authService.GetToken();
                    if (string.IsNullOrEmpty(token)) return;

                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (s, c, ch, e) => true
                    };

                    using var client = new HttpClient(handler);
                    client.BaseAddress = new Uri("https://localhost:7215/api/");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var data = new { role = newRole };
                    var json = JsonSerializer.Serialize(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PutAsync($"users/{id}/role", content);
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("✅ Роль изменена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        window.Close();
                        await LoadUsers();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Ошибка: {error}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            panel.Children.Add(saveBtn);

            window.Content = panel;
            window.ShowDialog();
        }
    }

    private async void DeleteUser_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int id)
        {
            var user = _users.Find(u => u.Id == id);
            if (user == null) return;

            var result = MessageBox.Show($"Удалить пользователя {user.Phone}?\n\nЭто действие необратимо!",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var token = _authService.GetToken();
                    if (string.IsNullOrEmpty(token)) return;

                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (s, c, ch, e) => true
                    };

                    using var client = new HttpClient(handler);
                    client.BaseAddress = new Uri("https://localhost:7215/api/");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var response = await client.DeleteAsync($"users/{id}");
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("✅ Пользователь удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadUsers();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Ошибка: {error}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}