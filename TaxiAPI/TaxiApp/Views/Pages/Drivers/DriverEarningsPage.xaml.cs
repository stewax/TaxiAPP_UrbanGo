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

public partial class DriverEarningsPage : UserControl
{
    private readonly MainViewModel _vm;
    private readonly IAuthService _authService;
    private List<DriverTripDto> _trips = new();

    public DriverEarningsPage(MainViewModel vm)
    {
        _vm = vm;
        var app = (App)Application.Current;
        _authService = (IAuthService)app.Services.GetService(typeof(IAuthService))!;

        InitializeComponent();
        Loaded += DriverEarningsPage_Loaded;
    }

    private async void DriverEarningsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadEarnings();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadEarnings();
    }

    private async void ApplyFilter_Click(object sender, RoutedEventArgs e)
    {
        await LoadEarningsByPeriod();
    }

    private async void ResetFilter_Click(object sender, RoutedEventArgs e)
    {
        DateFrom.SelectedDate = null;
        DateTo.SelectedDate = null;
        await LoadEarnings();
    }

    private async Task LoadEarnings()
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

            var response = await client.GetAsync("trips/driver");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<DriverEarningsResponse>(json, options);

                if (data != null)
                {
                    _trips = data.Trips;
                    TripsGrid.ItemsSource = _trips;

                    // Обновляем статистику
                    TotalIncomeText.Text = $"{data.TotalIncome:F0} ₽";
                    TotalTripsText.Text = data.TotalTrips.ToString();
                    TotalDistanceText.Text = $"{data.TotalDistance:F1} км";

                    System.Diagnostics.Debug.WriteLine($"✅ Загружено {data.TotalTrips} поездок, доход: {data.TotalIncome} ₽");
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка: {response.StatusCode} - {error}");
                MessageBox.Show($"Ошибка загрузки: {error}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Ошибка: {ex.Message}");
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadEarningsByPeriod()
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

            var url = "trips/driver/period?";
            if (DateFrom.SelectedDate.HasValue)
                url += $"from={DateFrom.SelectedDate.Value:yyyy-MM-dd}&";
            if (DateTo.SelectedDate.HasValue)
                url += $"to={DateTo.SelectedDate.Value:yyyy-MM-dd}";

            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<DriverEarningsResponse>(json, options);

                if (data != null)
                {
                    _trips = data.Trips;
                    TripsGrid.ItemsSource = _trips;

                    TotalIncomeText.Text = $"{data.TotalIncome:F0} ₽";
                    TotalTripsText.Text = data.TotalTrips.ToString();
                    TotalDistanceText.Text = $"{data.TotalDistance:F1} км";

                    System.Diagnostics.Debug.WriteLine($"✅ Загружено {data.TotalTrips} поездок за период");
                }
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