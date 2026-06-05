using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using TaxiApp.ViewModels;

namespace TaxiApp.Views.Pages;

public partial class OrderPage : UserControl
{
    private readonly MainViewModel _vm;
    private string? _pickupLat, _pickupLng;
    private string? _destLat, _destLng;
    private string _pickupAddress = "";
    private string _destAddress = "";
    private double _basePrice = 150;
    private double _routeDistance = 0; // Расстояние по дорогам (км)
    private int _routeDuration = 0;    // Время в минутах

    public OrderPage(MainViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await MapView.EnsureCoreWebView2Async(null);

            var mapPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Map.html");

            if (File.Exists(mapPath))
            {
                var htmlContent = await File.ReadAllTextAsync(mapPath);
                MapView.NavigateToString(htmlContent);
                MapView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            }
            else
            {
                MapView.NavigateToString("<h1 style='color:red; text-align:center;'>❌ Map.html not found</h1>");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка карты: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 🔍 Поиск точки подачи
    private async void FindPickup_Click(object sender, RoutedEventArgs e)
    {
        await GeocodeAddress(PickupAddress.Text, true);
    }

    // 🔍 Поиск точки назначения
    private async void FindDestination_Click(object sender, RoutedEventArgs e)
    {
        await GeocodeAddress(DestinationAddress.Text, false);
    }

    // 🌍 Геокодирование адреса
    private async Task GeocodeAddress(string address, bool isPickup)
    {
        if (string.IsNullOrWhiteSpace(address) || address.Contains("Введите адрес"))
        {
            MessageBox.Show("Введите адрес!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SearchIndicator.Visibility = Visibility.Visible;

        try
        {
            var encodedAddress = Uri.EscapeDataString(address);
            var url = $"https://nominatim.openstreetmap.org/search?format=json&q={encodedAddress}&limit=1";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "TaxiApp/1.0");

            var response = await client.GetStringAsync(url);
            var results = JsonDocument.Parse(response).RootElement;

            if (results.GetArrayLength() > 0)
            {
                var lat = results[0].GetProperty("lat").GetString();
                var lon = results[0].GetProperty("lon").GetString();
                var displayName = results[0].GetProperty("display_name").GetString();

                if (isPickup)
                {
                    _pickupLat = lat;
                    _pickupLng = lon;
                    _pickupAddress = displayName ?? address;
                    PickupAddress.Text = _pickupAddress;
                    PickupCoords.Text = $"Координаты: {lat}, {lon}";

                    MapView.CoreWebView2.PostWebMessageAsString($"pickup:{lat}:{lon}");
                }
                else
                {
                    _destLat = lat;
                    _destLng = lon;
                    _destAddress = displayName ?? address;
                    DestinationAddress.Text = _destAddress;
                    DestinationCoords.Text = $"Координаты: {lat}, {lon}";

                    MapView.CoreWebView2.PostWebMessageAsString($"destination:{lat}:{lon}");
                }
            }
            else
            {
                MessageBox.Show($"Адрес не найден: {address}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка поиска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SearchIndicator.Visibility = Visibility.Collapsed;
        }
    }

    // 💰 Обновление стоимости (теперь используем расстояние по дорогам)
    private void UpdateCost()
    {
        if (_routeDistance > 0)
        {
            var cost = _basePrice + (_routeDistance * 15); // 15 руб/км по дорогам
            EstimatedCost.Text = $"{(int)cost} ₽";
            DistanceText.Text = $"Расстояние: {_routeDistance:F1} км (~{_routeDuration} мин)";
        }
    }

    // 🎫 Изменение тарифа
    private void TariffSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TariffSelect.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            _basePrice = double.Parse(item.Tag.ToString()!, System.Globalization.CultureInfo.InvariantCulture);
            UpdateCost();
        }
    }

    // 🚖 Создание заказа
    private void CreateOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_pickupLat == null || _destLat == null)
        {
            MessageBox.Show("Укажите обе точки!\n\n1. Введите адрес подачи и нажмите 🔍\n2. Введите адрес назначения и нажмите 🔍",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var message = $"✅ Заказ создан!\n\n" +
                     $"📍 Подача: {_pickupAddress}\n" +
                     $"🎯 Назначение: {_destAddress}\n" +
                     $"💰 Стоимость: {EstimatedCost.Text}\n" +
                     $"📏 Расстояние: {_routeDistance:F1} км\n" +
                     $"⏱ Время: {_routeDuration} мин\n\n" +
                     $"Водитель будет назначен в ближайшее время.";

        MessageBox.Show(message, "Заказ принят", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // 🔄 Очистка
    private void ClearRoute_Click(object sender, RoutedEventArgs e)
    {
        _pickupLat = _pickupLng = _destLat = _destLng = null;
        _pickupAddress = _destAddress = "";
        _routeDistance = 0;
        _routeDuration = 0;

        PickupAddress.Text = "Введите адрес подачи...";
        DestinationAddress.Text = "Введите адрес назначения...";
        PickupCoords.Text = "Координаты: -";
        DestinationCoords.Text = "Координаты: -";
        DistanceText.Text = "Расстояние: -";
        EstimatedCost.Text = "0 ₽";

        MapView.CoreWebView2.PostWebMessageAsString("clear");
    }

    // 📨 Обработка сообщений от карты
    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = e.TryGetWebMessageAsString();

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (json.Contains("\"type\":\"route\""))
                {
                    // Парсим данные маршрута от OSRM
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("distance", out var distProp))
                    {
                        _routeDistance = distProp.GetDouble();
                    }

                    if (root.TryGetProperty("duration", out var durProp))
                    {
                        _routeDuration = durProp.GetInt32();
                    }

                    // Обновляем стоимость
                    UpdateCost();
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing web message: {ex.Message}");
        }
    }
}