using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TaxiApp.Models;
using TaxiApp.Services;
using TaxiApp.ViewModels;

namespace TaxiApp.Views.Pages.Admin;

public partial class AdminTripsPage : UserControl
{
    private readonly MainViewModel _vm;
    private readonly IAuthService _authService;

    public AdminTripsPage(MainViewModel vm)
    {
        _vm = vm;
        var app = (App)Application.Current;
        _authService = app.Services.GetRequiredService<IAuthService>();
        InitializeComponent();
        Loaded += async (s, e) => await LoadTrips();
    }

    private async Task LoadTrips()
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

            var response = await client.GetAsync("trips");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // Парсим как JsonElement для гибкости
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                List<AdminTripDto> trips;
                decimal totalIncome = 0;
                int totalTrips = 0;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    trips = JsonSerializer.Deserialize<List<AdminTripDto>>(json, options) ?? new();
                    totalIncome = trips.Sum(t => t.Price);
                    totalTrips = trips.Count;
                }
                else
                {
                    // Если объект с полями trips, totalIncome и т.д.
                    var tripsJson = root.GetProperty("trips").GetRawText();
                    trips = JsonSerializer.Deserialize<List<AdminTripDto>>(tripsJson, options) ?? new();
                    if (root.TryGetProperty("totalIncome", out var inc))
                        totalIncome = inc.GetDecimal();
                    if (root.TryGetProperty("totalTrips", out var cnt))
                        totalTrips = cnt.GetInt32();
                }

                TripsGrid.ItemsSource = trips;
                TotalIncomeText.Text = $"{totalIncome:F0} ₽";
                TotalTripsText.Text = totalTrips.ToString();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadTrips();
    }
}