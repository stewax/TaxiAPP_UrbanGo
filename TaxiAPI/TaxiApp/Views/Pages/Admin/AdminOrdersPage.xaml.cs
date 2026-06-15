using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TaxiApp.Models;
using TaxiApp.Services;
using TaxiApp.ViewModels;

namespace TaxiApp.Views.Pages.Admin;

public partial class AdminOrdersPage : UserControl
{
    private readonly MainViewModel _vm;
    private readonly IAuthService _authService;
    private List<AdminOrderDto> _allOrders = new();
    private List<AdminDriverDto> _availableDrivers = new();

    public AdminOrdersPage(MainViewModel vm)
    {
        _vm = vm;
        var app = (App)Application.Current;
        _authService = app.Services.GetRequiredService<IAuthService>();
        InitializeComponent();
        Loaded += async (s, e) => await LoadOrders();
    }

    private async Task LoadOrders()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🔄 Загрузка заказов...");

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

            System.Diagnostics.Debug.WriteLine("📤 Запрос GET api/orders");

            var response = await client.GetAsync("orders");

            System.Diagnostics.Debug.WriteLine($"📥 Ответ: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"📄 JSON длина: {json.Length} символов");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                _allOrders = JsonSerializer.Deserialize<List<AdminOrderDto>>(json, options) ?? new();

                System.Diagnostics.Debug.WriteLine($"✅ Загружено {_allOrders.Count} заказов");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (OrdersGrid != null)
                    {
                        // 🔥 ВАЖНО: Сначала удаляем старый обработчик, чтобы не было дубликатов
                        OrdersGrid.LoadingRow -= OrdersGrid_LoadingRow;

                        // 🔥 Подписываемся на событие загрузки строк
                        OrdersGrid.LoadingRow += OrdersGrid_LoadingRow;

                        // Устанавливаем источник данных
                        OrdersGrid.ItemsSource = null;
                        OrdersGrid.ItemsSource = _allOrders;

                        // 🔥 Применяем текущий фильтр, если он выбран
                        if (StatusFilter.SelectedItem is ComboBoxItem item)
                        {
                            var status = item.Content.ToString();
                            if (status != "Все заказы")
                            {
                                OrdersGrid.ItemsSource = _allOrders.Where(o => o.Status == status).ToList();
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ OrdersGrid равен null!");
                    }
                });
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                System.Diagnostics.Debug.WriteLine("❌ Endpoint /api/orders не найден (404)");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("API endpoint /api/orders не найден. Проверьте контроллер.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                System.Diagnostics.Debug.WriteLine("❌ Доступ запрещен (403)");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Недостаточно прав для просмотра заказов",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка {response.StatusCode}: {error}");
            }
        }
        catch (JsonException jsonEx)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Ошибка JSON: {jsonEx.Message}");
            System.Diagnostics.Debug.WriteLine($"📄 Stack: {jsonEx.StackTrace}");
        }
        catch (HttpRequestException httpEx)
        {
            System.Diagnostics.Debug.WriteLine($"❌ HTTP ошибка: {httpEx.Message}");
            System.Diagnostics.Debug.WriteLine("💡 Проверьте, запущен ли API сервер");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"💥 Критическая ошибка: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"📄 Stack: {ex.StackTrace}");

            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }

    // 🔥 НОВОЕ: Обработчик загрузки строк DataGrid
    private void OrdersGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is AdminOrderDto order)
        {
            // 🔥 Ждем полной загрузки строки
            e.Row.Loaded += (s, args) =>
            {
                try
                {
                    // 🔥 Ищем кнопку назначения водителя в строке
                    var assignButton = FindVisualChild<Button>(e.Row, "AssignDriverButton");

                    if (assignButton != null)
                    {
                        // 🔥 Скрываем кнопку если статус НЕ pending
                        if (order.Status == "pending")
                        {
                            assignButton.Visibility = Visibility.Visible;
                            System.Diagnostics.Debug.WriteLine($"✅ Заказ {order.Code}: кнопка назначения видима");
                        }
                        else
                        {
                            assignButton.Visibility = Visibility.Collapsed;
                            System.Diagnostics.Debug.WriteLine($"⚠️ Заказ {order.Code}: кнопка назначения скрыта (статус: {order.Status})");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Ошибка в LoadingRow: {ex.Message}");
                }
            };
        }
    }

    // 🔥 Вспомогательный метод для поиска элемента в визуальном дереве
    private T? FindVisualChild<T>(DependencyObject parent, string? name = null) where T : DependencyObject
    {
        if (parent == null) return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T typedChild)
            {
                // Если имя не указано или совпадает - возвращаем
                if (name == null || (child is FrameworkElement fe && fe.Name == name))
                {
                    return typedChild;
                }
            }

            // Рекурсивно ищем в дочерних элементах
            var result = FindVisualChild<T>(child, name);
            if (result != null) return result;
        }

        return null;
    }

    private async void AssignDriver_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int orderId)
        {
            var order = _allOrders.Find(o => o.Id == orderId);
            if (order == null) return;

            // 🔥 ПРОВЕРКА: Только для pending
            if (order.Status != "pending")
            {
                MessageBox.Show(
                    "Назначить водителя можно только на заказ со статусом 'pending'",
                    "Недоступно",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            // Загружаем список доступных водителей
            await LoadAvailableDrivers();

            if (_availableDrivers.Count == 0)
            {
                MessageBox.Show("Нет доступных водителей для назначения", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Создаем окно выбора водителя
            var window = new Window
            {
                Title = "Назначить водителя на заказ",
                Width = 450,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var panel = new StackPanel { Margin = new Thickness(20) };

            panel.Children.Add(new TextBlock
            {
                Text = $"Заказ: {order.Code}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"Откуда: {order.PickupAddress}",
                FontSize = 12,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 5)
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"Куда: {order.DestinationAddress}",
                FontSize = 12,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 15)
            });

            panel.Children.Add(new TextBlock
            {
                Text = "Выберите водителя:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var combo = new ComboBox
            {
                Width = 380,
                Padding = new Thickness(10, 8, 10, 8),
                FontSize = 13
            };

            foreach (var driver in _availableDrivers)
            {
                combo.Items.Add(new ComboBoxItem
                {
                    Content = $"🚗 {driver.FullName} ({driver.Phone}) - {driver.Code}",
                    Tag = driver.Id
                });
            }

            combo.SelectedIndex = 0;
            panel.Children.Add(combo);

            var saveBtn = new Button
            {
                Content = "✅ Назначить водителя",
                Margin = new Thickness(0, 20, 0, 0),
                Padding = new Thickness(15, 10, 15, 10),
                Background = System.Windows.Media.Brushes.Green,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            saveBtn.Click += async (s, ev) =>
            {
                if (combo.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is int driverId)
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

                        var data = new { driverId };
                        var json = JsonSerializer.Serialize(data);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await client.PutAsync($"orders/{orderId}/assign-driver", content);

                        if (response.IsSuccessStatusCode)
                        {
                            MessageBox.Show("✅ Водитель успешно назначен!\nЗаказ переведен в статус 'assigned'",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            window.Close();
                            await LoadOrders();
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
            };

            panel.Children.Add(saveBtn);
            window.Content = panel;
            window.ShowDialog();
        }
    }

    // 🔥 Загрузка списка доступных водителей
    private async Task LoadAvailableDrivers()
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

            var response = await client.GetAsync("orders/available-drivers");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                _availableDrivers = JsonSerializer.Deserialize<List<AdminDriverDto>>(json, options) ?? new();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($" Ошибка загрузки водителей: {ex.Message}");
        }
    }

    // 🔥 Просмотр деталей заказа
    private void ViewOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int orderId)
        {
            var order = _allOrders.Find(o => o.Id == orderId);
            if (order != null)
            {
                var info = $"📋 Информация о заказе\n\n" +
                          $"Код: {order.Code}\n" +
                          $"ID: {order.Id}\n" +
                          $"👤 Клиент: {order.ClientName} (ID: {order.ClientId})\n" +
                          $"🚗 Водитель: {(order.DriverName ?? "Не назначен")}\n" +
                          $"📍 Откуда: {order.PickupAddress}\n" +
                          $"🎯 Куда: {order.DestinationAddress}\n" +
                          $"💰 Стоимость: {order.EstimatedCost} ₽\n" +
                          $"📊 Статус: {order.Status}\n" +
                          $"📅 Создан: {order.CreatedAt:dd.MM.yyyy HH:mm}";

                MessageBox.Show(info, "Детали заказа", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    // 🔥 Изменение статуса заказа
    private async void ChangeStatus_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int orderId)
        {
            var order = _allOrders.Find(o => o.Id == orderId);
            if (order == null) return;

            var window = new Window
            {
                Title = "Изменить статус заказа",
                Width = 350,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock { Text = $"Заказ: {order.Code}", Margin = new Thickness(0, 0, 0, 10) });
            panel.Children.Add(new TextBlock { Text = "Выберите новый статус:", Margin = new Thickness(0, 0, 0, 10) });

            var combo = new ComboBox();
            combo.Items.Add("pending");
            combo.Items.Add("assigned");
            combo.Items.Add("in_progress");
            combo.Items.Add("completed");
            combo.Items.Add("cancelled");
            combo.SelectedItem = order.Status;
            panel.Children.Add(combo);

            var saveBtn = new Button { Content = "Сохранить", Margin = new Thickness(0, 15, 0, 0), Padding = new Thickness(10, 5, 10, 5) };
            saveBtn.Click += async (s, ev) =>
            {
                var newStatus = combo.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(newStatus)) return;

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

                    var data = new { status = newStatus };
                    var json = JsonSerializer.Serialize(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Предполагается, что есть endpoint PUT /api/orders/{id}/status
                    var response = await client.PutAsync($"orders/{orderId}/status", content);
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("✅ Статус изменен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        window.Close();
                        await LoadOrders(); // Перезагружаем список
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

    // 🔥 Удаление заказа
    private async void DeleteOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int orderId)
        {
            var order = _allOrders.Find(o => o.Id == orderId);
            if (order == null) return;

            var result = MessageBox.Show($"Удалить заказ {order.Code}?\n\nЭто действие необратимо!",
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

                    var response = await client.DeleteAsync($"orders/{orderId}");
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("✅ Заказ удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadOrders();
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

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadOrders();
    }

    private void StatusFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (OrdersGrid == null) return;

        if (StatusFilter.SelectedItem is ComboBoxItem item)
        {
            var status = item.Content.ToString();
            if (status == "Все заказы")
            {
                OrdersGrid.ItemsSource = _allOrders;
            }
            else
            {
                OrdersGrid.ItemsSource = _allOrders.Where(o => o.Status == status).ToList();
            }
        }
    }
}