using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using TaxiApp.Services;
using TaxiApp.ViewModels;

namespace TaxiApp.Views.Pages.Drivers;

public partial class DriverActiveOrderPage : UserControl
{
    private readonly MainViewModel _vm;
    private readonly IAuthService _authService;
    private int _orderId;
    private DateTime _tripStartTime;
    private string? _pickupLat, _pickupLng;
    private string? _destLat, _destLng;
    private bool _mapInitialized = false;

    public DriverActiveOrderPage(MainViewModel vm, int orderId)
    {
        _vm = vm;
        _orderId = orderId;
        var app = (App)Application.Current;
        _authService = (IAuthService)app.Services.GetService(typeof(IAuthService))!;

        InitializeComponent();
        Loaded += DriverActiveOrderPage_Loaded;
    }

    private async void DriverActiveOrderPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadOrderDetails();
    }

    private async Task LoadOrderDetails()
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

            var response = await client.GetAsync($"orders/{_orderId}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"📥 Полный JSON ответа: {json}");

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                OrderCode.Text = $"Код: {root.GetProperty("code").GetString()}";
                OrderStatus.Text = $"Статус: {root.GetProperty("status").GetString()}";

                if (root.TryGetProperty("client", out var clientProp) && clientProp.ValueKind != JsonValueKind.Null)
                {
                    System.Diagnostics.Debug.WriteLine($"👤 Клиент найден: {clientProp}");

                    if (clientProp.TryGetProperty("fullName", out var fullName))
                    {
                        ClientName.Text = $"Имя: {fullName.GetString()}";
                        System.Diagnostics.Debug.WriteLine($"✅ Имя клиента: {fullName.GetString()}");
                    }

                    if (clientProp.TryGetProperty("user", out var userProp) && userProp.ValueKind != JsonValueKind.Null)
                    {
                        if (userProp.TryGetProperty("phone", out var phone))
                        {
                            ClientPhone.Text = $"Телефон: {phone.GetString()}";
                            System.Diagnostics.Debug.WriteLine($"✅ Телефон клиента: {phone.GetString()}");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Клиент не найден в ответе");
                    ClientName.Text = "Имя: N/A";
                    ClientPhone.Text = "Телефон: N/A";
                }

                var pickupAddress = root.GetProperty("pickupAddress").GetString();
                var destAddress = root.GetProperty("destinationAddress").GetString();

                PickupAddress.Text = pickupAddress;
                DestinationAddress.Text = destAddress;

                var estimatedCost = root.GetProperty("estimatedCost").GetDecimal();
                EstimatedCost.Text = $"{estimatedCost} ₽";
                System.Diagnostics.Debug.WriteLine($"💰 Стоимость: {estimatedCost}");

                var status = root.GetProperty("status").GetString();
                if (status == "in_progress")
                {
                    StartTripButton.Visibility = Visibility.Collapsed;
                    CompleteTripButton.Visibility = Visibility.Visible;
                }
                else if (status == "assigned")
                {
                    StartTripButton.Visibility = Visibility.Visible;
                    CompleteTripButton.Visibility = Visibility.Collapsed;
                }

                System.Diagnostics.Debug.WriteLine("🗺 Инициализация карты...");
                await InitializeMap();

                System.Diagnostics.Debug.WriteLine("⏳ Ожидание готовности карты...");
                await Task.Delay(2000);

                System.Diagnostics.Debug.WriteLine("🔍 Начинаем геокодирование адресов...");

                if (!string.IsNullOrEmpty(pickupAddress))
                {
                    await GeocodeAndSendAddress(pickupAddress, true);
                    await Task.Delay(500);
                }

                if (!string.IsNullOrEmpty(destAddress))
                {
                    await GeocodeAndSendAddress(destAddress, false);
                }
            }
            // 🔥 НОВОЕ: Обработка ошибки 404 - перенаправление на страницу заказов
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                System.Diagnostics.Debug.WriteLine("❌ Заказ не найден (404). Перенаправление на страницу заказов...");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        "Этот заказ больше не доступен.\nВозможно, он был удален или уже завершен.",
                        "Заказ не найден",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    // Перенаправляем на страницу "Взять заказ"
                    _vm.NavigateTo("DriverOrders");
                });
            }
            // 🔥 НОВОЕ: Обработка других ошибок
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка API: {response.StatusCode} - {error}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"Не удалось загрузить заказ.\n\nКод ошибки: {response.StatusCode}\n\n" +
                        $"Вы будете перенаправлены на страницу заказов.",
                        "Ошибка загрузки",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );

                    // Перенаправляем на страницу "Взять заказ"
                    _vm.NavigateTo("DriverOrders");
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки заказа: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"📄 Stack: {ex.StackTrace}");

            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    $"Произошла ошибка при загрузке заказа.\n\n{ex.Message}\n\n" +
                    $"Вы будете перенаправлены на страницу заказов.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                // Перенаправляем на страницу "Взять заказ"
                _vm.NavigateTo("DriverOrders");
            });
        }
    }

    private async Task InitializeMap()
    {
        try
        {
            await MapView.EnsureCoreWebView2Async(null);

            var mapPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Map.html");

            if (File.Exists(mapPath))
            {
                var htmlContent = await File.ReadAllTextAsync(mapPath);

                // Используем TaskCompletionSource для ожидания загрузки
                var tcs = new TaskCompletionSource<bool>();

                MapView.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    _mapInitialized = true;
                    System.Diagnostics.Debug.WriteLine("✅ Карта инициализирована и готова");
                    tcs.TrySetResult(true);
                };

                MapView.NavigateToString(htmlContent);

                // Ждем завершения навигации (максимум 5 секунд)
                await Task.WhenAny(tcs.Task, Task.Delay(5000));

                if (!_mapInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Карта не инициализировалась за 5 секунд");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Файл карты не найден: {mapPath}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Ошибка инициализации карты: {ex.Message}");
        }
    }

    private async Task GeocodeAndSendAddress(string address, bool isPickup)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔍 Геокодирование: {address}");

            var encodedAddress = Uri.EscapeDataString(address);
            var url = $"https://nominatim.openstreetmap.org/search?format=json&q={encodedAddress}&limit=1&accept-language=ru";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "TaxiApp/1.0");

            var response = await client.GetStringAsync(url);
            var results = JsonDocument.Parse(response).RootElement;

            if (results.GetArrayLength() > 0)
            {
                var lat = results[0].GetProperty("lat").GetString();
                var lon = results[0].GetProperty("lon").GetString();

                System.Diagnostics.Debug.WriteLine($"✅ Координаты: {lat}, {lon}");

                if (isPickup)
                {
                    _pickupLat = lat;
                    _pickupLng = lon;
                }
                else
                {
                    _destLat = lat;
                    _destLng = lon;
                }

                // Отправляем на карту
                if (_mapInitialized && MapView.CoreWebView2 != null)
                {
                    var message = isPickup ? $"pickup:{lat}:{lon}" : $"destination:{lat}:{lon}";
                    MapView.CoreWebView2.PostWebMessageAsString(message);
                    System.Diagnostics.Debug.WriteLine($"📤 Отправлена {message}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Карта не готова! _mapInitialized={_mapInitialized}, CoreWebView2={MapView.CoreWebView2 != null}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Адрес не найден: {address}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Ошибка геокодирования: {ex.Message}");
        }
    }

    private async void StartTrip_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Начать поездку?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
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

                var response = await client.PutAsync($"orders/{_orderId}/start", null);

                if (response.IsSuccessStatusCode)
                {
                    _tripStartTime = DateTime.Now;
                    MessageBox.Show("✅ Поездка начата!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    StartTripButton.Visibility = Visibility.Collapsed;
                    CompleteTripButton.Visibility = Visibility.Visible;
                    OrderStatus.Text = "Статус: in_progress";
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

    private async void CompleteTrip_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Завершить поездку?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
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

                var costText = EstimatedCost.Text.Replace(" ₽", "").Replace("₽", "").Trim();
                if (!decimal.TryParse(costText,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var finalCost))
                {
                    if (!decimal.TryParse(costText, out finalCost))
                    {
                        finalCost = 0;
                    }
                }

                if (finalCost > 10000)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Исправляем цену: {finalCost} → {finalCost / 100}");
                    finalCost = finalCost / 100;
                }

                var distance = 10.5;
                if (!string.IsNullOrEmpty(_pickupLat) && !string.IsNullOrEmpty(_destLat))
                {
                    try
                    {
                        var lat1 = double.Parse(_pickupLat.Replace(',', '.'),
                            System.Globalization.CultureInfo.InvariantCulture);
                        var lon1 = double.Parse(_pickupLng.Replace(',', '.'),
                            System.Globalization.CultureInfo.InvariantCulture);
                        var lat2 = double.Parse(_destLat.Replace(',', '.'),
                            System.Globalization.CultureInfo.InvariantCulture);
                        var lon2 = double.Parse(_destLng.Replace(',', '.'),
                            System.Globalization.CultureInfo.InvariantCulture);

                        distance = CalculateDistance(lat1, lon1, lat2, lon2);
                        System.Diagnostics.Debug.WriteLine($"📏 Расстояние рассчитано: {distance:F2} км");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Ошибка расчета расстояния: {ex.Message}");
                    }
                }

                var duration = (int)(DateTime.Now - _tripStartTime).TotalMinutes;
                if (duration <= 0) duration = 1;

                var completeData = new
                {
                    finalCost = Math.Round(finalCost, 2),
                    distanceKm = Math.Round(distance, 2),
                    durationMinutes = duration,
                    startTime = _tripStartTime,
                    endTime = DateTime.Now
                };

                System.Diagnostics.Debug.WriteLine($"📤 Отправка данных завершения:");
                System.Diagnostics.Debug.WriteLine($"  Стоимость: {completeData.finalCost}");
                System.Diagnostics.Debug.WriteLine($"  Расстояние: {completeData.distanceKm} км");
                System.Diagnostics.Debug.WriteLine($"  Длительность: {completeData.durationMinutes} мин");

                var json = JsonSerializer.Serialize(completeData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"📤 JSON: {json}");

                var response = await client.PutAsync($"orders/{_orderId}/complete", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"📥 Ответ сервера: {response.StatusCode} - {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("✅ Поездка завершена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    _vm.NavigateTo("DriverOrders");
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
                System.Diagnostics.Debug.WriteLine($"📄 Stack: {ex.StackTrace}");
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;

        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        _vm.NavigateTo("DriverOrders");
    }
}