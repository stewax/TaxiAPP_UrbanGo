using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TaxiApp.Services;
using TaxiApp.ViewModels;

namespace TaxiApp.Views.Pages;

public partial class HistoryPage : UserControl
{
    private readonly MainViewModel _vm;

    public HistoryPage(MainViewModel vm)
    {
        _vm = vm;
        InitializeComponent();

        // Загружаем историю при открытии страницы
        Loaded += async (s, e) => await LoadOrdersAsync();
    }

    private async Task LoadOrdersAsync()
    {
        try
        {
            // Показываем индикатор загрузки (если есть ProgressBar в XAML)
            // loadingIndicator.Visibility = Visibility.Visible;

            var app = (App)Application.Current;
            var apiService = app.Services.GetService(typeof(IApiService)) as IApiService;

            if (apiService == null)
            {
                MessageBox.Show("Сервис API недоступен.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 🔥 Получаем заказы текущего клиента через API
            var orders = await apiService.GetMyOrdersAsync(_vm.CurrentUserId);

            if (orders != null && orders.Any())
            {
                // Преобразуем модели в анонимный тип для DataGrid
                TripsGrid.ItemsSource = orders.Select(o => new
                {
                    o.Code,
                    CreatedAt = o.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                    PickupAddress = o.PickupAddress,
                    DestinationAddress = o.DestinationAddress,
                    EstimatedCost = $"{o.EstimatedCost} ₽",
                    Status = GetStatusText(o.Status)
                }).ToList();
            }
            else
            {
                // Если заказов нет — показываем пустой список с сообщением
                TripsGrid.ItemsSource = new List<object>();
                MessageBox.Show("У вас пока нет поездок.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show("Сессия истекла. Войдите заново.", "Авторизация",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки истории:\n{ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            System.Diagnostics.Debug.WriteLine($"❌ LoadOrdersAsync error: {ex}");
        }
        finally
        {
            // loadingIndicator.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Переводит статус заказа на русский язык
    /// </summary>
    private string GetStatusText(string status)
    {
        return status?.ToLower() switch
        {
            "pending" => "Поиск водителя",
            "assigned" => "Водитель найден",
            "in_progress" => "В пути",
            "completed" => "Завершен",
            "cancelled" => "Отменен",
            _ => status ?? "Неизвестно"
        };
    }
}