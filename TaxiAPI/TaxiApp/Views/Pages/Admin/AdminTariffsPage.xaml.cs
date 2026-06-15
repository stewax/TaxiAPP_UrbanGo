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

public partial class AdminTariffsPage : UserControl
{
    private readonly MainViewModel _vm;
    private readonly IAuthService _authService;
    private List<AdminTariffDto> _tariffs = new();
    private int? _editingId = null;

    public AdminTariffsPage(MainViewModel vm)
    {
        _vm = vm;
        var app = (App)Application.Current;
        _authService = app.Services.GetRequiredService<IAuthService>();
        InitializeComponent();
        Loaded += async (s, e) => await LoadTariffs();
    }

    private async Task LoadTariffs()
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

            var response = await client.GetAsync("tariffs");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                _tariffs = JsonSerializer.Deserialize<List<AdminTariffDto>>(json, options) ?? new();
                TariffsGrid.ItemsSource = _tariffs;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddTariff_Click(object sender, RoutedEventArgs e)
    {
        _editingId = null;
        TariffName.Text = "";
        TariffBasePrice.Text = "";
        TariffPricePerKm.Text = "";
        EditPanel.Visibility = Visibility.Visible;
    }

    private void EditTariff_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int id)
        {
            var tariff = _tariffs.Find(t => t.Id == id);
            if (tariff != null)
            {
                _editingId = id;
                TariffName.Text = tariff.Name;
                TariffBasePrice.Text = tariff.BasePrice.ToString();
                TariffPricePerKm.Text = tariff.PricePerKm.ToString();
                EditPanel.Visibility = Visibility.Visible;
            }
        }
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        EditPanel.Visibility = Visibility.Collapsed;
        _editingId = null;
    }

    private async void SaveTariff_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TariffName.Text))
        {
            MessageBox.Show("Введите название тарифа", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TariffBasePrice.Text, out var basePrice) ||
            !decimal.TryParse(TariffPricePerKm.Text, out var pricePerKm))
        {
            MessageBox.Show("Введите корректные цены", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

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

            var data = new { name = TariffName.Text, basePrice, pricePerKm };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            if (_editingId.HasValue)
            {
                response = await client.PutAsync($"tariffs/{_editingId.Value}", content);
            }
            else
            {
                response = await client.PostAsync("tariffs", content);
            }

            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show(_editingId.HasValue ? "✅ Тариф обновлен" : "✅ Тариф добавлен",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                EditPanel.Visibility = Visibility.Collapsed;
                _editingId = null;
                await LoadTariffs();
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

    private async void DeleteTariff_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int id)
        {
            var result = MessageBox.Show("Удалить этот тариф?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

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

                    var response = await client.DeleteAsync($"tariffs/{id}");
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("✅ Тариф удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadTariffs();
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