using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TaxiApp.Models;
using TaxiApp.Services;
using TaxiApp.ViewModels;

namespace TaxiApp.Views.Pages.Drivers;

public partial class DriverOrdersPage : UserControl
{
    private readonly MainViewModel _vm;
    private readonly IAuthService _authService;
    private List<AvailableOrderDto> _orders = new();

    public DriverOrdersPage(MainViewModel vm)
    {
        _vm = vm;
        var app = (App)Application.Current;
        _authService = (IAuthService)app.Services.GetService(typeof(IAuthService))!;

        InitializeComponent();
        Loaded += DriverOrdersPage_Loaded;
    }

    private async void DriverOrdersPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAvailableOrders();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadAvailableOrders();
    }

    private async Task LoadAvailableOrders()
    {
        try
        {
            var token = _authService.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                MessageBox.Show("Сессия истекла", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            using var client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://localhost:7215/api/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("orders/available");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                // 🔥 Десериализуем в список DTO
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                _orders = JsonSerializer.Deserialize<List<AvailableOrderDto>>(json, options) ?? new();

                // 🔥 Привязываем к DataGrid
                OrdersGrid.ItemsSource = _orders;

                System.Diagnostics.Debug.WriteLine($"✅ Загружено {_orders.Count} заказов");
            }
            else
            {
                MessageBox.Show($"Ошибка загрузки: {response.StatusCode}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($" Ошибка: {ex.Message}");
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void TakeOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int orderId)
        {
            var result = MessageBox.Show("Взять этот заказ?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await TakeOrder(orderId);
            }
        }
    }

    private async Task TakeOrder(int orderId)
    {
        try
        {
            var token = _authService.GetToken();
            if (string.IsNullOrEmpty(token)) return;

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            using var client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://localhost:7215/api/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsync($"orders/{orderId}/take", null);

            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("✅ Заказ успешно взят!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Переходим на страницу активного заказа
                _vm.NavigateTo("DriverActiveOrder", orderId);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Ошибка: {error}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}