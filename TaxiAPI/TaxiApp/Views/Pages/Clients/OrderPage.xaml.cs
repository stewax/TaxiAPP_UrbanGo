using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using TaxiApp.ViewModels;
using TaxiAPI.Models;
using TaxiApp.Services;
using TaxiAPI.DTOs;

namespace TaxiApp.Views.Pages;

public partial class OrderPage : UserControl
{
    private readonly MainViewModel _vm;
    private string? _pickupLat, _pickupLng;
    private string? _destLat, _destLng;
    private string _pickupAddress = "";
    private string _destAddress = "";
    private double _basePrice = 150;
    private double _routeDistance = 0;
    private int _routeDuration = 0;
    private List<Tariff>? _availableTariffs;
    private Tariff? _selectedTariff;

    private readonly Dictionary<string, CachedResult> _addressCache = new();
    private readonly HttpClient _httpClient;

    public OrderPage(MainViewModel vm)
    {
        _vm = vm;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "TaxiApp/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);

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

                System.Diagnostics.Debug.WriteLine("✅ WebView2 инициализирован");
            }

            // 🔥 Загружаем тарифы при открытии страницы
            await LoadTariffs();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка карты: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private async Task LoadTariffs()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🔄 Загрузка тарифов...");

            // Получаем сервис API через MainViewModel или напрямую
            var app = (App)Application.Current;
            var apiService = app.Services.GetService(typeof(IApiService)) as IApiService;

            if (apiService != null)
            {
                _availableTariffs = await apiService.GetTariffsAsync();

                if (_availableTariffs != null && _availableTariffs.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Загружено {_availableTariffs.Count} тарифов");

                    // Очищаем и заполняем ComboBox
                    TariffSelect.Items.Clear();

                    foreach (var tariff in _availableTariffs)
                    {
                        if (tariff.Status == "active")
                        {
                            TariffSelect.Items.Add(tariff);
                        }
                    }

                    // Выбираем первый тариф по умолчанию
                    if (TariffSelect.Items.Count > 0)
                    {
                        TariffSelect.SelectedIndex = 0;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Тарифы не найдены, используем заглушки");
                    UseFallbackTariffs();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ IApiService не найден");
                UseFallbackTariffs();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки тарифов: {ex.Message}");
            UseFallbackTariffs();
        }
    }

    // Запасные тарифы если API недоступен
    private void UseFallbackTariffs()
    {
        TariffSelect.Items.Clear();
        TariffSelect.Items.Add(new Tariff { Id = 1, Name = "Эконом", BasePrice = 150, PricePerKm = 15 });
        TariffSelect.Items.Add(new Tariff { Id = 2, Name = "Комфорт", BasePrice = 250, PricePerKm = 20 });
        TariffSelect.Items.Add(new Tariff { Id = 3, Name = "Бизнес", BasePrice = 500, PricePerKm = 30 });
        TariffSelect.SelectedIndex = 0;
    }

    private async void FindPickup_Click(object sender, RoutedEventArgs e)
    {
        await GeocodeAddress(PickupAddress.Text, true);
    }

    private async void FindDestination_Click(object sender, RoutedEventArgs e)
    {
        await GeocodeAddress(DestinationAddress.Text, false);
    }

    private async Task GeocodeAddress(string address, bool isPickup)
    {
        if (string.IsNullOrWhiteSpace(address) || address.Contains("Введите адрес"))
        {
            MessageBox.Show("Введите адрес!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var cacheKey = address.ToLower().Trim();
        if (_addressCache.TryGetValue(cacheKey, out var cached))
        {
            SetAddressResult(cached.Lat, cached.Lon, cached.FullAddress, isPickup);
            return;
        }

        SearchIndicator.Visibility = Visibility.Visible;

        try
        {
            var result = await TryMultipleGeocoders(address);

            if (result != null)
            {
                _addressCache[cacheKey] = result;
                SetAddressResult(result.Lat, result.Lon, result.FullAddress, isPickup);
            }
            else
            {
                MessageBox.Show($"Адрес не найден: {address}\n\nПопробуйте ввести адрес точнее.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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

    private async Task<CachedResult?> TryMultipleGeocoders(string address)
    {
        var result = await GeocodeWithNominatim(address);
        if (result != null) return result;

        if (!address.Contains("Россия", StringComparison.OrdinalIgnoreCase))
        {
            result = await GeocodeWithNominatim($"{address}, Россия");
            if (result != null) return result;
        }

        return null;
    }

    private async Task<CachedResult?> GeocodeWithNominatim(string address)
    {
        try
        {
            var encodedAddress = Uri.EscapeDataString(address);
            var url = $"https://nominatim.openstreetmap.org/search?format=json&q={encodedAddress}&limit=5&accept-language=ru&addressdetails=1";

            var response = await _httpClient.GetStringAsync(url);
            var results = JsonDocument.Parse(response).RootElement;

            if (results.GetArrayLength() > 0)
            {
                var bestResult = SelectBestResult(results);
                var lat = bestResult.GetProperty("lat").GetString();
                var lon = bestResult.GetProperty("lon").GetString();
                var fullAddress = FormatAddressFromNominatim(bestResult);

                return new CachedResult { Lat = lat!, Lon = lon!, FullAddress = fullAddress };
            }
        }
        catch { }

        return null;
    }

    private JsonElement SelectBestResult(JsonElement results)
    {
        var addressTypes = new[] { "house", "building", "residential", "street", "road", "neighbourhood", "suburb", "city" };

        foreach (var type in addressTypes)
        {
            foreach (var result in results.EnumerateArray())
            {
                if (result.TryGetProperty("address", out var address))
                {
                    if (address.TryGetProperty(type, out _))
                    {
                        return result;
                    }
                }
            }
        }

        return results[0];
    }

    private string FormatAddressFromNominatim(JsonElement result)
    {
        var parts = new List<string>();

        if (result.TryGetProperty("address", out var address))
        {
            var addressOrder = new[] { "house_number", "road", "street", "neighbourhood", "suburb", "city", "town", "village", "state", "country" };

            foreach (var key in addressOrder)
            {
                if (address.TryGetProperty(key, out var prop))
                {
                    var value = prop.GetString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        parts.Add(value);
                    }
                }
            }
        }

        if (parts.Count == 0 && result.TryGetProperty("display_name", out var displayName))
        {
            return displayName.GetString()!;
        }

        return string.Join(", ", parts);
    }

    private void SetAddressResult(string lat, string lon, string fullAddress, bool isPickup)
    {
        if (isPickup)
        {
            _pickupLat = lat;
            _pickupLng = lon;
            _pickupAddress = fullAddress;
            PickupAddress.Text = _pickupAddress;
            PickupCoords.Text = $"Координаты: {lat}, {lon}";
            SafePostToMap($"pickup:{lat}:{lon}");
        }
        else
        {
            _destLat = lat;
            _destLng = lon;
            _destAddress = fullAddress;
            DestinationAddress.Text = _destAddress;
            DestinationCoords.Text = $"Координаты: {lat}, {lon}";
            SafePostToMap($"destination:{lat}:{lon}");
        }
    }

    // Безопасная отправка сообщения в карту
    private void SafePostToMap(string message)
    {
        try
        {
            if (MapView.CoreWebView2 != null)
            {
                MapView.CoreWebView2.PostWebMessageAsString(message);
                System.Diagnostics.Debug.WriteLine($" Отправлено в карту: {message}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ CoreWebView2 еще не готов, сообщение не отправлено: {message}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($" Ошибка отправки в карту: {ex.Message}");
        }
    }

    // Обновление стоимости
    private void UpdateCost()
    {
        System.Diagnostics.Debug.WriteLine($"🔄 UpdateCost: distance={_routeDistance}, selectedTariff={_selectedTariff?.Name}");

        if (_routeDistance > 0 && _selectedTariff != null)
        {
            //  ИСПРАВЛЕНИЕ: Приводим decimal к double для расчетов
            var basePrice = (double)_selectedTariff.BasePrice;
            var pricePerKm = (double)_selectedTariff.PricePerKm;
            var minPrice = (double)_selectedTariff.MinPrice;

            // Считаем стоимость
            var cost = basePrice + (_routeDistance * pricePerKm);

            // Учитываем минимальную стоимость
            if (cost < minPrice)
            {
                cost = minPrice;
            }

            EstimatedCost.Text = $"{(int)cost} ₽";
            DistanceText.Text = $"Расстояние: {_routeDistance:F1} км (~{_routeDuration} мин)";

            System.Diagnostics.Debug.WriteLine($"✅ Стоимость: {EstimatedCost.Text} (тариф: {_selectedTariff.Name})");
        }
        else
        {
            EstimatedCost.Text = "0 ₽";
            DistanceText.Text = "Расстояние: -";
        }
    }

    private void TariffSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TariffSelect.SelectedItem is Tariff tariff)
        {
            _selectedTariff = tariff;
            _basePrice = (double)tariff.BasePrice;

            System.Diagnostics.Debug.WriteLine($"🎫 Выбран тариф: {tariff.Name}, база={tariff.BasePrice}, км={tariff.PricePerKm}");

            // Пересчитываем стоимость если маршрут уже построен
            UpdateCost();
        }
    }

    private async void CreateOrder_Click(object sender, RoutedEventArgs e)
    {
        // 1. Базовая валидация
        if (_pickupLat == null || _destLat == null)
        {
            MessageBox.Show("Укажите обе точки на карте!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_selectedTariff == null)
        {
            MessageBox.Show("Выберите тариф!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var btn = (Button)sender;
        var originalContent = btn.Content;
        btn.Content = "⏳ Отправка...";
        btn.IsEnabled = false;

        try
        {
            // Парсим стоимость из текста
            var costText = EstimatedCost.Text.Replace(" ₽", "").Replace(" ", "");
            if (!decimal.TryParse(costText, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal estimatedCost))
            {
                MessageBox.Show("Некорректная сумма заказа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Формируем DTO
            var orderDto = new OrderCreateDto
            {
                ClientId = _vm.CurrentUserId,
                TariffId = _selectedTariff.Id,
                PickupAddress = _pickupAddress,
                DestinationAddress = _destAddress,
                EstimatedCost = estimatedCost
            };

            System.Diagnostics.Debug.WriteLine($"📤 Отправляем заказ: {System.Text.Json.JsonSerializer.Serialize(orderDto)}");

            // Получаем сервис API
            var app = (App)Application.Current;
            var apiService = app.Services.GetService(typeof(IApiService)) as IApiService;

            if (apiService == null)
            {
                throw new Exception("Сервис IApiService не зарегистрирован в DI контейнере!");
            }

            // Вызываем API
            var createdOrder = await apiService.CreateOrderAsync(orderDto);

            if (createdOrder != null)
            {
                MessageBox.Show(
                    $"✅ Заказ создан!\n\n" +
                    $"Код: {createdOrder.Code}\n" +
                    $"Статус: Поиск водителя\n" +
                    $"Сумма: {createdOrder.EstimatedCost} ₽",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearRoute_Click(null!, null!);
            }
            else
            {
                // ⚠️ ВАЖНО: Показываем реальную ошибку вместо "попробуйте позже"
                MessageBox.Show(
                    "❌ Не удалось создать заказ.\n\n" +
                    "Возможные причины:\n" +
                    "• API сервер не запущен\n" +
                    "• Ошибка авторизации (токен истек)\n" +
                    "• Сервер вернул ошибку\n\n" +
                    "Проверьте окно Output (Debug) для деталей.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show("🔒 Сессия истекла. Пожалуйста, войдите заново.", "Авторизация",
                MessageBoxButton.OK, MessageBoxImage.Warning);

            // Автоматический выход
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            foreach (Window w in Application.Current.Windows)
                if (w is MainWindow) { w.Close(); break; }
        }
        catch (HttpRequestException ex)
        {
            MessageBox.Show(
                $"🌐 Ошибка подключения к серверу:\n\n{ex.Message}\n\n" +
                "Проверьте:\n" +
                "• Запущен ли API сервер\n" +
                "• Правильный ли URL в ApiService.cs\n" +
                "• Подключение к интернету",
                "Ошибка сети", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"❌ Произошла ошибка:\n\n{ex.Message}\n\n{ex.StackTrace}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

            System.Diagnostics.Debug.WriteLine($" КРИТИЧЕСКАЯ ОШИБКА: {ex}");
        }
        finally
        {
            btn.Content = originalContent;
            btn.IsEnabled = true;
        }
    }

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

        SafePostToMap("clear");
    }

    // ИСПРАВЛЕННЫЙ обработчик сообщений от карты
    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            // Получаем сообщение как строку
            var messageString = e.TryGetWebMessageAsString();

            System.Diagnostics.Debug.WriteLine($" === ПОЛУЧЕНО СООБЩЕНИЕ ===");
            System.Diagnostics.Debug.WriteLine($"Текст: {messageString}");

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Проверяем, что это JSON
                    if (messageString.StartsWith("{"))
                    {
                        using var doc = JsonDocument.Parse(messageString);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("type", out var typeProp))
                        {
                            var messageType = typeProp.GetString();
                            System.Diagnostics.Debug.WriteLine($"📋 Тип сообщения: {messageType}");

                            if (messageType == "route")
                            {
                                System.Diagnostics.Debug.WriteLine("✅ Это данные маршрута!");

                                if (root.TryGetProperty("distance", out var distProp))
                                {
                                    // Пробуем разные способы получения значения
                                    if (distProp.ValueKind == JsonValueKind.String)
                                    {
                                        _routeDistance = double.Parse(distProp.GetString()!,
                                            System.Globalization.CultureInfo.InvariantCulture);
                                    }
                                    else
                                    {
                                        _routeDistance = distProp.GetDouble();
                                    }
                                    System.Diagnostics.Debug.WriteLine($"📏 Расстояние получено: {_routeDistance} км");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("❌ Свойство distance не найдено");
                                }

                                if (root.TryGetProperty("duration", out var durProp))
                                {
                                    if (durProp.ValueKind == JsonValueKind.String)
                                    {
                                        _routeDuration = int.Parse(durProp.GetString()!,
                                            System.Globalization.CultureInfo.InvariantCulture);
                                    }
                                    else
                                    {
                                        _routeDuration = durProp.GetInt32();
                                    }
                                    System.Diagnostics.Debug.WriteLine($"⏱ Время получено: {_routeDuration} мин");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("❌ Свойство duration не найдено");
                                }

                                // ВАЖНО: Вызываем UpdateCost после получения данных
                                UpdateCost();

                                // Показываем результат
                                System.Diagnostics.Debug.WriteLine($"✅ ИТОГО: {_routeDistance} км, {_routeDuration} мин, {EstimatedCost.Text}");
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"️ Это не JSON: {messageString}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($" Ошибка парсинга: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"📄 Stack: {ex.StackTrace}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Ошибка получения: {ex.Message}");
        }
    }
}

public class CachedResult
{
    public string Lat { get; set; } = "";
    public string Lon { get; set; } = "";
    public string FullAddress { get; set; } = "";
}